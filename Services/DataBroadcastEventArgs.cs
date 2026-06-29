using ISO11820.Models;

namespace ISO11820.Services;

/// <summary>
/// 数据广播事件参数
/// </summary>
public class DataBroadcastEventArgs : EventArgs
{
    /// <summary>5通道温度值（sensorId → temperature）</summary>
    public Dictionary<int, double> Temperatures { get; set; } = new();

    /// <summary>当前状态中文描述</summary>
    public string Status { get; set; } = "";

    /// <summary>已记录秒数</summary>
    public int ElapsedSeconds { get; set; }

    /// <summary>已记录的数据点数</summary>
    public int RecordedCount { get; set; }

    /// <summary>温度漂移（°C/10min）</summary>
    public double TemperatureDrift { get; set; }

    /// <summary>新增消息列表</summary>
    public List<MasterMessage> Messages { get; set; } = new();

    /// <summary>当前状态枚举</summary>
    public Core.TestState CurrentState { get; set; }
}