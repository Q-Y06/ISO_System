namespace ISO11820.Models;

/// <summary>
/// 设备模型 — 对应 apparatus 表
/// </summary>
public class Apparatus
{
    public int ApparatusId { get; set; }
    public string InnerNumber { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;
    public DateTime CheckDateF { get; set; }
    public DateTime CheckDateT { get; set; }
    public string PidPort { get; set; } = string.Empty;
    public string PowerPort { get; set; } = string.Empty;
    public int? ConstPower { get; set; }
}
