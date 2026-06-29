namespace ISO11820.Models;

/// <summary>
/// 设备信息
/// </summary>
public class Apparatus
{
    public int ApparatusId { get; set; }
    public string InnerNumber { get; set; } = "";
    public string ApparatusName { get; set; } = "";
    public DateTime CheckDateF { get; set; }
    public DateTime CheckDateT { get; set; }
    public string PidPort { get; set; } = "";
    public string PowerPort { get; set; } = "";
    public int? ConstPower { get; set; }
}