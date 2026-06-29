namespace ISO11820.Services;

/// <summary>
/// 仿真引擎 — 生成5通道温度数据
///
/// 通道映射：
///   0  = TF1 (炉温1)
///   1  = TF2 (炉温2)
///   2  = TS  (表面温度)
///   3  = TC  (中心温度)
///   16 = TCal (校准温度)
/// </summary>
public class SensorSimulator
{
    private readonly Random _random = new();
    private double _tf1;
    private double _tf2;
    private double _ts;
    private double _tc;
    private bool _isRecording;
    private bool _isCooling;

    public SensorSimulator(double initialTemp)
    {
        _tf1 = initialTemp;
        _tf2 = initialTemp;
        _ts = initialTemp * 0.3;
        _tc = initialTemp * 0.25;
    }

    public void SetRecording(bool recording)
    {
        _isRecording = recording;
    }

    public void SetCooling(bool cooling)
    {
        _isCooling = cooling;
    }

    /// <summary>
    /// 每800ms调用一次，返回 sensorId -> temperature 的字典
    /// </summary>
    public Dictionary<int, double> Update()
    {
        double targetTemp = Config.AppConfig.TargetFurnaceTemp;
        double heatingRate = Config.AppConfig.HeatingRatePerSecond;
        double fluctuation = Config.AppConfig.TempFluctuation;

        double step = heatingRate * 0.8; // 每800ms
        double noise() => (_random.NextDouble() * 2 - 1) * fluctuation;

        if (_isCooling)
        {
            // 降温阶段
            _tf1 -= 0.5 + _random.NextDouble() * 0.1;
            _tf2 -= 0.5 + _random.NextDouble() * 0.1;
            _tf1 = Math.Max(_tf1, 25);
            _tf2 = Math.Max(_tf2, 25);
        }
        else if (_tf1 < targetTemp - Config.AppConfig.StableThreshold)
        {
            // 升温阶段（TF1 < 747°C）
            _tf1 += step + noise();
            _tf2 += step + noise();
        }
        else
        {
            // 稳定阶段（TF1 >= 747°C）
            _tf1 = targetTemp + noise();
            _tf2 = targetTemp + noise();
        }

        if (_isRecording)
        {
            // 记录阶段：TS和TC指数接近炉温
            double surfaceTarget = Math.Min(_tf1 * 0.95, 800);
            _ts += (surfaceTarget - _ts) * 0.02 + noise();

            double centerTarget = Math.Min(_tf1 * 0.85, 750);
            _tc += (centerTarget - _tc) * 0.01 + noise();
        }
        else
        {
            // 非记录阶段：低值跟随
            _ts = _tf1 * 0.3 + noise();
            _tc = _tf1 * 0.25 + noise();
        }

        double tcal = _tf1 + (_random.NextDouble() * 2 - 1) * fluctuation * 2;

        return new Dictionary<int, double>
        {
            [0] = Math.Round(_tf1, 1),
            [1] = Math.Round(_tf2, 1),
            [2] = Math.Round(_ts, 1),
            [3] = Math.Round(_tc, 1),
            [16] = Math.Round(tcal, 1)
        };
    }
}