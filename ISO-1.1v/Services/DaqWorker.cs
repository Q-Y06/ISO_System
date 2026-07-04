using ISO11820.Models;
using System.Diagnostics;

namespace ISO11820.Services;

public class DaqWorker : IDisposable
{
    private System.Windows.Forms.Timer? _timer;
    private readonly SensorSimulator _simulator;
    private readonly SimulationConfig _config;
    private Dictionary<string, double> _temperatures;
    private readonly List<double> _furnaceTempHistory = new();
    private readonly List<MasterMessage> _pendingMessages = new();

    private readonly Stopwatch _stopwatch = new();       // 系统运行总计时
    private long _recordingStartMs;                       // 记录开始时刻

    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;
    public event Action? SecondElapsed;

    public Dictionary<string, double> Temperatures => _temperatures;
    public TestState CurrentState { get; set; } = TestState.Idle;

    // 记录阶段已过秒数
    public int ElapsedSeconds => CurrentState == TestState.Recording
        ? (int)((_stopwatch.ElapsedMilliseconds - _recordingStartMs) / 1000)
        : 0;

    // 系统总运行秒数（始终递增，用于调试）
    public int TotalRunSeconds => (int)(_stopwatch.ElapsedMilliseconds / 1000);

    public DaqWorker(SensorSimulator simulator, SimulationConfig config)
    {
        _simulator = simulator;
        _config = config;
        _temperatures = simulator.GetInitialTemperatures();
    }

    public void Start()
    {
        if (_timer == null)
        {
            _timer = new System.Windows.Forms.Timer { Interval = 800 };
            _timer.Tick += OnTick;
        }
        _stopwatch.Start();
        _timer.Start();
    }

    public void Stop() => _timer?.Stop();

    public void ResetElapsed()
    {
        _recordingStartMs = _stopwatch.ElapsedMilliseconds;
    }

    private int _lastDisplayedSecond = -1;

    private void OnTick(object? sender, EventArgs e)
    {
        // 仿真模式：更新5通道温度
        if (_config.EnableSimulation)
            _temperatures = _simulator.Update(_temperatures, CurrentState);

        // 记录炉温历史
        _furnaceTempHistory.Add(_temperatures["TF1"]);
        if (_furnaceTempHistory.Count > 600) _furnaceTempHistory.RemoveAt(0);

        // 秒检测
        int currentSecond = TotalRunSeconds;
        if (currentSecond != _lastDisplayedSecond)
        {
            _lastDisplayedSecond = currentSecond;
            SecondElapsed?.Invoke();
        }

        // 计算温漂
        double drift = DriftCalculator.CalculateDrift(_furnaceTempHistory);

        // 广播数据
        var args = new DataBroadcastEventArgs
        {
            Temperatures = new Dictionary<string, double>(_temperatures),
            CurrentState = CurrentState.ToString(),
            ElapsedSeconds = ElapsedSeconds,
            IsStable = false,
            Drift = drift,
            Messages = new List<MasterMessage>(_pendingMessages)
        };

        DataBroadcast?.Invoke(this, args);
        _pendingMessages.Clear();
    }

    public void AddMessage(string message)
    {
        _pendingMessages.Add(new MasterMessage
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Message = message
        });
    }

    public List<double> GetFurnaceTempHistory() => new(_furnaceTempHistory);
    public double GetCurrentDrift() => DriftCalculator.CalculateDrift(_furnaceTempHistory);

    public void Dispose()
    {
        _stopwatch.Stop();
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
