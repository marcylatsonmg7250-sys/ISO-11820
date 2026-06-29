namespace ISO11820.Models;

/// <summary>
/// 系统消息（不入库，仅用于UI显示）
/// </summary>
public class MasterMessage
{
    public string Time { get; set; } = "";      // 格式 HH:mm:ss
    public string Message { get; set; } = "";    // 消息内容
}