using ISO11820.Core;
using ISO11820.Global;
using Serilog;

namespace ISO11820.Services;

/// <summary>
/// 数据采集工作线程 — 每 800ms 调用 TestController.OnDaqTick()
/// </summary>
public class DaqWorker : IDisposable
{
    private readonly TestController _controller;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public DaqWorker(TestController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// 启动后台采集循环
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        _isRunning = true;
        _workerTask = Task.Run(() => RunLoop(_cts.Token));
        Log.Information("DaqWorker 已启动，周期={Ms}ms", AppConstants.DaqIntervalMs);
    }

    /// <summary>
    /// 停止后台采集循环
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _cts?.Cancel();
        _workerTask?.Wait(TimeSpan.FromSeconds(3));
        _isRunning = false;
        Log.Information("DaqWorker 已停止");
    }

    /// <summary>
    /// 主循环
    /// </summary>
    private async Task RunLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var startTime = DateTime.Now;

                try
                {
                    _controller.OnDaqTick();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "DaqWorker.OnDaqTick 异常");
                }

                // 计算等待时间（800ms 减去执行耗时）
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var delay = Math.Max(10, AppConstants.DaqIntervalMs - elapsed);

                try
                {
                    await Task.Delay((int)delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DaqWorker 主循环异常");
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
