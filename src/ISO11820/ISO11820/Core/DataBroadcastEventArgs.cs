using ISO11820.Models;

namespace ISO11820.Core;

/// <summary>
/// 数据广播事件参数 — 每 800ms 触发一次，携带所有实时数据
/// </summary>
public class DataBroadcastEventArgs : EventArgs
{
    /// <summary>当前试验状态</summary>
    public TestState CurrentState { get; set; }

    /// <summary>炉温1 (°C)</summary>
    public double TF1 { get; set; }

    /// <summary>炉温2 (°C)</summary>
    public double TF2 { get; set; }

    /// <summary>表面温度 (°C)</summary>
    public double TS { get; set; }

    /// <summary>中心温度 (°C)</summary>
    public double TC { get; set; }

    /// <summary>校准温度 (°C)</summary>
    public double TCal { get; set; }

    /// <summary>记录阶段已过秒数</summary>
    public int ElapsedSeconds { get; set; }

    /// <summary>温度漂移 (°C/10min)</summary>
    public double TemperatureDrift { get; set; }

    /// <summary>本次 tick 产生的系统消息</summary>
    public List<MasterMessage> Messages { get; set; } = new();

    /// <summary>事件时间戳</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>当前样品编号（可能为空）</summary>
    public string? ProductId { get; set; }

    /// <summary>当前试验ID（可能为空）</summary>
    public string? TestId { get; set; }
}
