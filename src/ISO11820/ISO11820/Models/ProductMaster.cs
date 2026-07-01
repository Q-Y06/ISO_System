namespace ISO11820.Models;

/// <summary>
/// 样品模型 — 对应 productmaster 表
/// </summary>
public class ProductMaster
{
    public string ProductId { get; set; } = string.Empty;  // 主键
    public string ProductName { get; set; } = string.Empty;
    public string Specific { get; set; } = string.Empty;
    public double Diameter { get; set; }
    public double Height { get; set; }
    public string? Flag { get; set; }
}
