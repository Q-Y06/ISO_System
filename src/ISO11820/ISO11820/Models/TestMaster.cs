namespace ISO11820.Models;

/// <summary>
/// 试验记录模型 — 对应 testmaster 表（核心表）
/// 联合主键：(ProductId, TestId)
/// </summary>
public class TestMaster
{
    // ===== 基本信息 =====
    public string ProductId { get; set; } = string.Empty;      // 样品编号（联合主键 + 外键）
    public string TestId { get; set; } = string.Empty;         // 试验ID，格式 yyyyMMdd-HHmmss
    public DateTime TestDate { get; set; }                      // 试验日期
    public double AmbTemp { get; set; }                         // 环境温度 (°C)
    public double AmbHumi { get; set; }                         // 环境湿度 (%)
    public string According { get; set; } = "ISO 11820:2022";  // 试验依据
    public string Operator { get; set; } = string.Empty;       // 操作员用户名
    public string ApparatusId { get; set; } = string.Empty;    // 设备编号
    public string ApparatusName { get; set; } = string.Empty;  // 设备名称
    public DateTime ApparatusChkDate { get; set; }              // 设备检定日期
    public string RptNo { get; set; } = string.Empty;          // 报告编号

    // ===== 质量数据 =====
    public double PreWeight { get; set; }                       // 试验前质量 (g)
    public double PostWeight { get; set; }                      // 试验后质量 (g)
    public double LostWeight { get; set; }                      // 失重量
    public double LostWeightPer { get; set; }                   // 失重率 (%)

    // ===== 试验过程 =====
    public int TotalTestTime { get; set; }                      // 总试验时长（秒）
    public int ConstPower { get; set; }                         // 恒功率值
    public string PhenoCode { get; set; } = string.Empty;      // 现象编码
    public int FlameTime { get; set; }                          // 火焰开始时刻（秒）
    public int FlameDuration { get; set; }                      // 火焰持续时间（秒）

    // ===== 各通道温度最大值 =====
    public double MaxTf1 { get; set; }
    public double MaxTf2 { get; set; }
    public double MaxTs { get; set; }
    public double MaxTc { get; set; }
    public int MaxTf1Time { get; set; }
    public int MaxTf2Time { get; set; }
    public int MaxTsTime { get; set; }
    public int MaxTcTime { get; set; }

    // ===== 各通道温度最终值 =====
    public double FinalTf1 { get; set; }
    public double FinalTf2 { get; set; }
    public double FinalTs { get; set; }
    public double FinalTc { get; set; }
    public int FinalTf1Time { get; set; }
    public int FinalTf2Time { get; set; }
    public int FinalTsTime { get; set; }
    public int FinalTcTime { get; set; }

    // ===== 温升 =====
    public double DeltaTf1 { get; set; }
    public double DeltaTf2 { get; set; }
    public double DeltaTf { get; set; }     // 样品温升（取表面温升）
    public double DeltaTs { get; set; }
    public double DeltaTc { get; set; }

    // ===== 备注 =====
    public string? Memo { get; set; }
    public string? Flag { get; set; }       // "10000000" 表示已保存
}
