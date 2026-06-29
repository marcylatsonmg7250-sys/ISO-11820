using ISO11820.Core;

namespace ISO11820.Services;

/// <summary>
/// 数据采集服务 — 每800ms运行一次，桥接仿真引擎和试验控制器
/// </summary>
public class DaqWorker : IDisposable
{
    private readonly SensorSimulator _simulator;
    private readonly TestMaster _testMaster;
    private System.Threading.Timer? _timer;
    private bool _running;

    public DaqWorker(TestMaster testMaster)
    {
        _testMaster = testMaster;
        double initialTemp = Config.AppConfig.InitialFurnaceTemp;
        _simulator = new SensorSimulator(initialTemp);
        _testMaster.Initialize(initialTemp);
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _timer = new System.Threading.Timer(Tick, null, 0, 800); // 每800ms
        Serilog.Log.Information("DaqWorker已启动，采集周期800ms");
    }

    public void Stop()
    {
        _running = false;
        _timer?.Dispose();
        _timer = null;
    }

    public void SetRecording(bool recording)
    {
        _simulator.SetRecording(recording);
    }

    public void SetCooling(bool cooling)
    {
        _simulator.SetCooling(cooling);
    }

    private void Tick(object? state)
    {
        if (!_running) return;

        try
        {
            // 1. 更新仿真温度
            var sensorValues = _simulator.Update();

            // 2. 更新试验控制器（状态机）
            _testMaster.DoWork(sensorValues);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "DaqWorker tick error");
        }
    }

    public void Dispose()
    {
        Stop();
        Serilog.Log.Debug("DaqWorker已停止");
    }
}