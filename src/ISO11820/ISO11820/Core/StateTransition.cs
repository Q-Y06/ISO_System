namespace ISO11820.Core;

/// <summary>
/// 状态转换验证 — 检查状态转换是否合法
/// </summary>
public static class StateTransition
{
    /// <summary>
    /// 允许的转换：
    /// Idle     -> Preparing   (用户点击「开始升温」)
    /// Preparing -> Ready       (自动：温度稳定)
    /// Preparing -> Idle        (用户点击「停止升温」)
    /// Ready    -> Recording   (用户点击「开始记录」)
    /// Ready    -> Preparing   (自动：温度跌出稳定范围)
    /// Ready    -> Idle        (用户点击「停止升温」)
    /// Recording -> Complete   (自动：到达时长 或 用户手动停止)
    /// Complete -> Preparing   (用户点击「停止升温」后自动)
    /// </summary>
    public static bool CanTransition(TestState from, TestState to)
    {
        return (from, to) switch
        {
            (TestState.Idle, TestState.Preparing)       => true,
            (TestState.Preparing, TestState.Ready)       => true,
            (TestState.Preparing, TestState.Idle)        => true,
            (TestState.Ready, TestState.Recording)       => true,
            (TestState.Ready, TestState.Preparing)       => true,
            (TestState.Ready, TestState.Idle)            => true,
            (TestState.Recording, TestState.Complete)    => true,
            (TestState.Complete, TestState.Preparing)    => true,
            _ => false
        };
    }
}
