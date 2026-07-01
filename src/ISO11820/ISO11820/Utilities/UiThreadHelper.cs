namespace ISO11820.Utilities;

/// <summary>
/// UI 线程安全的 Invoke 封装
/// </summary>
public static class UiThreadHelper
{
    /// <summary>
    /// 如果当前不在 UI 线程，则用 Invoke 切换；否则直接执行
    /// </summary>
    public static void InvokeIfNeeded(Control control, Action action)
    {
        if (control.InvokeRequired)
            control.Invoke(action);
        else
            action();
    }

    /// <summary>
    /// 异步 BeginInvoke（不阻塞调用线程）
    /// </summary>
    public static void BeginInvokeIfNeeded(Control control, Action action)
    {
        if (control.InvokeRequired)
            control.BeginInvoke(action);
        else
            action();
    }
}
