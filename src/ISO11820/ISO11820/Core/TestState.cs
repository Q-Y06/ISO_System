namespace ISO11820.Core;

/// <summary>
/// 试验状态枚举 — 5 个状态
/// </summary>
public enum TestState
{
    /// <summary>空闲 — 无活动试验，炉子关闭</summary>
    Idle,

    /// <summary>升温中 — 正在加热到 750°C</summary>
    Preparing,

    /// <summary>就绪 — 温度稳定在 745~755°C，等待开始记录</summary>
    Ready,

    /// <summary>记录中 — 正在记录温度数据</summary>
    Recording,

    /// <summary>完成 — 试验结束，等待保存结果</summary>
    Complete
}
