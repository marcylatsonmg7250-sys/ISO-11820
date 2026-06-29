using ISO11820.Models;
using ISO11820.Services;

namespace ISO11820.Core;

/// <summary>
/// 试验状态枚举
/// </summary>
public enum TestState
{
    Idle,       // 空闲
    Preparing,  // 升温中
    Ready,      // 就绪（温度稳定，可开始记录）
    Recording,  // 记录中
    Complete    // 完成
}

/// <summary>
/// 试验时长模式
/// </summary>
public enum TestDurationMode
{
    Standard60Min,  // 标准60分钟
    FixedDuration   // 固定时长
}

/// <summary>
/// 试验控制器 — 状态机核心
/// </summary>
public class TestMaster
{
    // ==================== 事件 ====================
    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;
    public event EventHandler? StateChanged;

    // ==================== 状态 ====================
    public TestState CurrentState { get; private set; } = TestState.Idle;

    // ==================== 当前试验信息 ====================
    public string? CurrentProductId { get; private set; }
    public string? CurrentTestId { get; private set; }
    public string? CurrentOperator { get; private set; }
    public double PreWeight { get; private set; }
    public double AmbTemp { get; private set; }
    public double AmbHumi { get; private set; }
    public string? ProductName { get; private set; }
    public TestDurationMode DurationMode { get; private set; } = TestDurationMode.Standard60Min;
    public int TargetDurationSeconds { get; private set; } = 3600;

    // ==================== 运行时数据 ====================
    public int ElapsedSeconds { get; private set; }
    public int StableCounter { get; private set; }
    public bool IsStable { get; private set; }
    public double TemperatureDrift { get; private set; }
    public int ConstPowerValue { get; private set; } = 2048;

    // 温度历史队列（最近10分钟，600个点，每个800ms）
    private readonly Queue<double> _tf1History = new();
    private readonly Queue<double> _tf2History = new();
    private const int MaxHistoryPoints = 750; // 750 * 0.8s = 600s = 10分钟

    // PID输出队列（用于恒功率计算）
    private readonly Queue<double> _pidOutputQueue = new();
    private const int MaxPidPoints = 600;

    // 记录阶段的温度数据缓存
    public List<TemperatureRecord> RecordingData { get; } = new();

    // 传感器当前值
    public Dictionary<int, double> SensorValues { get; } = new();

    // 各通道最大值追踪
    public double MaxTf1 { get; private set; }
    public double MaxTf2 { get; private set; }
    public double MaxTs { get; private set; }
    public double MaxTc { get; private set; }
    public int MaxTf1Time { get; private set; }
    public int MaxTf2Time { get; private set; }
    public int MaxTsTime { get; private set; }
    public int MaxTcTime { get; private set; }

    // 记录开始时的温度值（用于计算温升）
    private double _recordStartTf1;
    private double _recordStartTf2;
    private double _recordStartTs;
    private double _recordStartTc;

    // 实时计时（使用DateTime替代tick计数）
    private DateTime _recordStartTime;
    private int _lastRecordedSecond = -1;

    // CSV文件路径
    public string? SensorDataFilePath { get; private set; }

    private readonly Random _random = new();
    private readonly object _lock = new();

    // ==================== 初始化 ====================

    public void Initialize(double initialTemp)
    {
        SensorValues[0] = initialTemp;   // TF1
        SensorValues[1] = initialTemp;   // TF2
        SensorValues[2] = initialTemp * 0.3;  // TS
        SensorValues[3] = initialTemp * 0.25; // TC
        SensorValues[16] = initialTemp;  // TCal

        ConstPowerValue = Config.AppConfig.ConstPower;
    }

    // ==================== 状态转换方法 ====================

    /// <summary>
    /// 设置当前试验信息（不切换状态，仅记录数据）
    /// </summary>
    public bool SetCurrentTest(string productId, string testId, string operatorName,
                                double preWeight, double ambTemp, double ambHumi, string productName,
                                TestDurationMode mode, int targetSeconds)
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Idle && CurrentState != TestState.Preparing)
                return false;

            CurrentProductId = productId;
            CurrentTestId = testId;
            CurrentOperator = operatorName;
            PreWeight = preWeight;
            AmbTemp = ambTemp;
            AmbHumi = ambHumi;
            ProductName = productName;
            DurationMode = mode;
            TargetDurationSeconds = mode == TestDurationMode.Standard60Min ? 3600 : targetSeconds;

            ElapsedSeconds = 0;
            StableCounter = 0;
            IsStable = false;
            RecordingData.Clear();
            _tf1History.Clear();
            _tf2History.Clear();
            _pidOutputQueue.Clear();
            ResetMaxValues();

            // 触发状态变化事件，让UI更新按钮状态
            StateChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }

    public bool StartHeating()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Idle)
                return false;

            if (string.IsNullOrEmpty(CurrentProductId))
                return false;

            CurrentState = TestState.Preparing;
            AddMessage("开始升温，系统升温中");
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }

    public bool StopHeating()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Preparing && CurrentState != TestState.Ready && CurrentState != TestState.Complete)
                return false;

            CurrentState = TestState.Idle;
            AddMessage("停止升温，系统回到空闲状态");
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }

    public bool StartRecording()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Ready)
                return false;

            // 记录开始时的温度
            _recordStartTf1 = SensorValues.GetValueOrDefault(0, 0);
            _recordStartTf2 = SensorValues.GetValueOrDefault(1, 0);
            _recordStartTs = SensorValues.GetValueOrDefault(2, 0);
            _recordStartTc = SensorValues.GetValueOrDefault(3, 0);

            // 计算恒功率
            if (_pidOutputQueue.Count > 0)
                ConstPowerValue = (int)_pidOutputQueue.Average();
            else
                ConstPowerValue = Config.AppConfig.ConstPower;

            ElapsedSeconds = 0;
            _recordStartTime = DateTime.Now;
            _lastRecordedSecond = -1;
            RecordingData.Clear();

            // 创建CSV文件路径
            string baseDir = Config.AppConfig.TestDataDirectory;
            string csvDir = Path.Combine(baseDir, CurrentProductId ?? "unknown", CurrentTestId ?? "unknown");
            Directory.CreateDirectory(csvDir);
            SensorDataFilePath = Path.Combine(csvDir, "sensor_data.csv");

            CurrentState = TestState.Recording;
            AddMessage("开始记录，计时开始");
            Serilog.Log.Information("开始记录: ProductId={Pid}, TestId={Tid}, CSV={Path}",
                CurrentProductId, CurrentTestId, SensorDataFilePath);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }

    public bool StopRecording()
    {
        lock (_lock)
        {
            if (CurrentState != TestState.Recording)
                return false;

            if (RecordingData.Count > 0)
            {
                CurrentState = TestState.Complete;
                AddMessage("用户手动停止记录");
            }
            else
            {
                // 没有有效记录，回到就绪状态
                CurrentState = TestState.Ready;
                AddMessage("无有效记录，回到就绪状态");
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }

    public void SaveRecordsAndReset()
    {
        lock (_lock)
        {
            CurrentState = TestState.Preparing;
            ElapsedSeconds = 0;
            StableCounter = 0;
            IsStable = false;
            RecordingData.Clear();
            _tf1History.Clear();
            _tf2History.Clear();
            _pidOutputQueue.Clear();
            ResetMaxValues();
            CurrentProductId = null;
            CurrentTestId = null;
            SensorDataFilePath = null;
        }
    }

    public void ResetToIdle()
    {
        lock (_lock)
        {
            CurrentState = TestState.Idle;
            ElapsedSeconds = 0;
            StableCounter = 0;
            IsStable = false;
            RecordingData.Clear();
            _tf1History.Clear();
            _tf2History.Clear();
            _pidOutputQueue.Clear();
            ResetMaxValues();
            CurrentProductId = null;
            CurrentTestId = null;
            SensorDataFilePath = null;
        }
    }

    private void ResetMaxValues()
    {
        MaxTf1 = 0; MaxTf2 = 0; MaxTs = 0; MaxTc = 0;
        MaxTf1Time = 0; MaxTf2Time = 0; MaxTsTime = 0; MaxTcTime = 0;
    }

    // ==================== DoWork（由 DaqWorker 每800ms调用） ====================

    public void DoWork(Dictionary<int, double> sensorValues)
    {
        lock (_lock)
        {
            // 更新传感器值
            foreach (var kv in sensorValues)
                SensorValues[kv.Key] = kv.Value;

            double tf1 = SensorValues.GetValueOrDefault(0, 0);
            double tf2 = SensorValues.GetValueOrDefault(1, 0);
            double ts = SensorValues.GetValueOrDefault(2, 0);
            double tc = SensorValues.GetValueOrDefault(3, 0);

            // 更新温度历史
            _tf1History.Enqueue(tf1);
            _tf2History.Enqueue(tf2);
            while (_tf1History.Count > MaxHistoryPoints) _tf1History.Dequeue();
            while (_tf2History.Count > MaxHistoryPoints) _tf2History.Dequeue();

            // 模拟PID输出
            double pidOutput = 0.5 + (Config.AppConfig.TargetFurnaceTemp - tf1) / Config.AppConfig.TargetFurnaceTemp * 0.5;
            pidOutput = Math.Clamp(pidOutput, 0, 1);
            _pidOutputQueue.Enqueue(pidOutput * Config.AppConfig.ConstPower);
            while (_pidOutputQueue.Count > MaxPidPoints) _pidOutputQueue.Dequeue();

            // 计算温漂
            CalculateTemperatureDrift();

            // 状态机逻辑
            switch (CurrentState)
            {
                case TestState.Preparing:
                    CheckStartCriteria(tf1);
                    break;

                case TestState.Ready:
                    // 检查温度是否跌出稳定范围
                    if (tf1 < Config.AppConfig.StableTempMin || tf1 > Config.AppConfig.StableTempMax)
                    {
                        IsStable = false;
                        StableCounter = 0;
                        CurrentState = TestState.Preparing;
                        AddMessage("温度偏离稳定范围，重新升温");
                        StateChanged?.Invoke(this, EventArgs.Empty);
                    }
                    break;

                case TestState.Recording:
                    ProcessRecording();
                    break;
            }

            // 触发数据广播
            var messages = CollectMessages();
            DataBroadcast?.Invoke(this, new DataBroadcastEventArgs
            {
                Temperatures = new Dictionary<int, double>(SensorValues),
                Status = GetStatusText(),
                ElapsedSeconds = ElapsedSeconds,
                RecordedCount = RecordingData.Count,
                TemperatureDrift = TemperatureDrift,
                Messages = messages,
                CurrentState = CurrentState
            });
        }
    }

    private void CheckStartCriteria(double tf1)
    {
        // 检查温度是否进入稳定范围
        if (tf1 >= Config.AppConfig.StableTempMin && tf1 <= Config.AppConfig.StableTempMax)
        {
            StableCounter++;
            if (StableCounter > 3) // 约3.2秒
            {
                IsStable = true;
                CurrentState = TestState.Ready;
                AddMessage("温度已稳定，可以开始记录");
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            StableCounter = 0;
            IsStable = false;
        }
    }

    private void ProcessRecording()
    {
        double tf1 = SensorValues.GetValueOrDefault(0, 0);
        double tf2 = SensorValues.GetValueOrDefault(1, 0);
        double ts = SensorValues.GetValueOrDefault(2, 0);
        double tc = SensorValues.GetValueOrDefault(3, 0);
        double tcal = SensorValues.GetValueOrDefault(16, 0);

        // 基于真实时间计算已过秒数
        int realSeconds = (int)(DateTime.Now - _recordStartTime).TotalSeconds;
        ElapsedSeconds = realSeconds;

        // 每秒记录一次（避免800ms tick导致重复记录）
        if (realSeconds > _lastRecordedSecond)
        {
            _lastRecordedSecond = realSeconds;

            // 追踪最大值
            TrackMaxValues(tf1, tf2, ts, tc);

            // 添加到记录缓存
            RecordingData.Add(new TemperatureRecord
            {
                Time = realSeconds,
                Temp1 = Math.Round(tf1, 1),
                Temp2 = Math.Round(tf2, 1),
                TempSurface = Math.Round(ts, 1),
                TempCenter = Math.Round(tc, 1),
                TempCalibration = Math.Round(tcal, 1)
            });

            // 写入CSV
            AppendToCsv(realSeconds, tf1, tf2, ts, tc, tcal);

            // 每10秒输出一次日志确认记录正常
            if (realSeconds % 10 == 0)
                Serilog.Log.Debug("记录中: {Count}条数据, {Sec}秒, TF1={Tf1:F1}°C",
                    RecordingData.Count, realSeconds, tf1);

            // 检查终止条件
            CheckTerminationCriteria();
        }
    }

    private void TrackMaxValues(double tf1, double tf2, double ts, double tc)
    {
        if (tf1 > MaxTf1) { MaxTf1 = tf1; MaxTf1Time = ElapsedSeconds; }
        if (tf2 > MaxTf2) { MaxTf2 = tf2; MaxTf2Time = ElapsedSeconds; }
        if (ts > MaxTs) { MaxTs = ts; MaxTsTime = ElapsedSeconds; }
        if (tc > MaxTc) { MaxTc = tc; MaxTcTime = ElapsedSeconds; }
    }

    private void CheckTerminationCriteria()
    {
        bool shouldTerminate = false;
        string? reason = null;

        if (DurationMode == TestDurationMode.Standard60Min)
        {
            // 60分钟无条件终止
            if (ElapsedSeconds >= 3600)
            {
                shouldTerminate = true;
                reason = $"记录时间到达 {ElapsedSeconds} 秒，试验自动结束";
            }
            // 每5分钟检查终止条件（30, 35, 40, 45, 50, 55分钟）
            else if (ElapsedSeconds >= 1800 && ElapsedSeconds % 300 == 0)
            {
                // 检查温漂条件
                if (_tf1History.Count >= 600 && _tf2History.Count >= 600)
                {
                    double drift1 = Math.Abs(CalculateDrift(_tf1History));
                    double drift2 = Math.Abs(CalculateDrift(_tf2History));
                    if (drift1 < 2.0 && drift2 < 2.0)
                    {
                        shouldTerminate = true;
                        reason = "满足终止条件，试验结束";
                    }
                }
            }
        }
        else
        {
            // 固定时长模式
            if (ElapsedSeconds >= TargetDurationSeconds)
            {
                shouldTerminate = true;
                reason = $"达到目标时长 {TargetDurationSeconds} 秒，试验自动结束";
            }
        }

        if (shouldTerminate)
        {
            CurrentState = TestState.Complete;
            AddMessage(reason ?? "试验结束");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CalculateTemperatureDrift()
    {
        if (_tf1History.Count >= 600)
        {
            TemperatureDrift = CalculateDrift(_tf1History);
        }
    }

    private double CalculateDrift(Queue<double> history)
    {
        var values = history.ToArray();
        if (values.Length < 2) return 0;

        // 使用 MathNet.Numerics 线性回归
        double[] x = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();
        var (intercept, slope) = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(x, values);
        return slope * 600; // 转换为 °C/10min (600个点 = 10分钟 = 600*0.8s)
    }

    private void AppendToCsv(int time, double tf1, double tf2, double ts, double tc, double tcal)
    {
        if (string.IsNullOrEmpty(SensorDataFilePath)) return;

        try
        {
            string? dir = Path.GetDirectoryName(SensorDataFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool writeHeader = !File.Exists(SensorDataFilePath);
            using var writer = new StreamWriter(SensorDataFilePath, append: true);
            if (writeHeader)
                writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
            writer.WriteLine($"{time},{tf1:F1},{tf2:F1},{ts:F1},{tc:F1},{tcal:F1}");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "CSV写入失败: {Path}", SensorDataFilePath);
        }
    }

    // ==================== 消息系统 ====================

    private readonly List<MasterMessage> _pendingMessages = new();

    private void AddMessage(string message)
    {
        _pendingMessages.Add(new MasterMessage
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Message = message
        });
    }

    private List<MasterMessage> CollectMessages()
    {
        var msgs = new List<MasterMessage>(_pendingMessages);
        _pendingMessages.Clear();
        return msgs;
    }

    // ==================== 辅助方法 ====================

    public string GetStatusText()
    {
        return CurrentState switch
        {
            TestState.Idle => "空闲",
            TestState.Preparing => "升温中",
            TestState.Ready => "就绪",
            TestState.Recording => "记录中",
            TestState.Complete => "完成",
            _ => "未知"
        };
    }

    public double GetFinalTf1() => SensorValues.GetValueOrDefault(0, 0);
    public double GetFinalTf2() => SensorValues.GetValueOrDefault(1, 0);
    public double GetFinalTs() => SensorValues.GetValueOrDefault(2, 0);
    public double GetFinalTc() => SensorValues.GetValueOrDefault(3, 0);

    public double GetRecordStartTf1() => _recordStartTf1;
    public double GetRecordStartTf2() => _recordStartTf2;
    public double GetRecordStartTs() => _recordStartTs;
    public double GetRecordStartTc() => _recordStartTc;
}

/// <summary>
/// 温度记录点
/// </summary>
public class TemperatureRecord
{
    public int Time { get; set; }
    public double Temp1 { get; set; }
    public double Temp2 { get; set; }
    public double TempSurface { get; set; }
    public double TempCenter { get; set; }
    public double TempCalibration { get; set; }
}