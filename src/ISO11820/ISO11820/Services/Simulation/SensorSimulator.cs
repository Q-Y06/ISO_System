using ISO11820.Core;
using ISO11820.Global;

namespace ISO11820.Services.Simulation;

/// <summary>
/// 温度仿真引擎 — 生成 5 通道温度数据
/// 每 800ms 调用一次 Update()
/// </summary>
public class SensorSimulator
{
    private readonly SimulationConfig _config;
    private readonly Random _rng = new();

    // 当前温度值
    public double TF1 { get; private set; }
    public double TF2 { get; private set; }
    public double TS { get; private set; }
    public double TC { get; private set; }
    public double TCal { get; private set; }

    // 稳定计数器
    public int StableTickCount { get; private set; }
    public bool IsStable => StableTickCount >= 3;

    // PID 输出值历史（用于计算恒功率）
    private readonly Queue<double> _pidHistory = new(600);
    public double AveragePidOutput
    {
        get
        {
            if (_pidHistory.Count == 0) return _config.HeatingRatePerSecond > 0 ? 2048 : 0;
            return _pidHistory.Average();
        }
    }

    // 炉温历史（用于温漂计算）
    private readonly Queue<(int Time, double Temp)> _tf1History = new(600);
    private readonly Queue<(int Time, double Temp)> _tf2History = new(600);

    public SensorSimulator(SimulationConfig config)
    {
        _config = config;
        TF1 = config.InitialFurnaceTemp;
        TF2 = config.InitialFurnaceTemp;
        TS = config.InitialFurnaceTemp * 0.3;
        TC = config.InitialFurnaceTemp * 0.25;
        TCal = config.InitialFurnaceTemp;
    }

    /// <summary>
    /// 每 800ms 执行一次的主更新方法
    /// </summary>
    public void Update(TestState state, bool isRecording, int elapsedSeconds)
    {
        switch (state)
        {
            case TestState.Idle:
                CoolDown();
                break;
            case TestState.Preparing:
                HeatUp();
                break;
            case TestState.Ready:
                MaintainFurnaceTemperature();
                UpdateIdleSampleTemperatures();
                break;
            case TestState.Recording:
                MaintainFurnaceTemperature();
                UpdateRecordingSampleTemperatures(elapsedSeconds);
                break;
            case TestState.Complete:
                MaintainFurnaceTemperature();
                UpdateIdleSampleTemperatures();
                break;
        }

        // 校准温度始终 = TF1 + 噪声×2
        TCal = TF1 + RandomNoise() * 2;

        // 记录 PID 输出历史（仿真用 TF1 的稳定程度模拟 PID 输出）
        if (state != TestState.Idle)
        {
            double simulatedPid = _config.TargetFurnaceTemp - TF1;
            _pidHistory.Enqueue(Math.Max(0, 2048 + simulatedPid * 10));
            if (_pidHistory.Count > 600) _pidHistory.Dequeue();
        }

        // 记录炉温历史（用于温漂计算）
        if (state == TestState.Ready || state == TestState.Recording)
        {
            RecordTemperatureHistory(elapsedSeconds);
        }
    }

    /// <summary>
    /// 获取炉温1历史数据（用于温漂计算）
    /// </summary>
    public List<(double Time, double Temp)> GetTF1History()
    {
        return _tf1History.Select(p => ((double)p.Time, p.Temp)).ToList();
    }

    /// <summary>
    /// 获取炉温2历史数据（用于温漂计算）
    /// </summary>
    public List<(double Time, double Temp)> GetTF2History()
    {
        return _tf2History.Select(p => ((double)p.Time, p.Temp)).ToList();
    }

    /// <summary>
    /// 重置稳定计数器
    /// </summary>
    public void ResetStableCounter()
    {
        StableTickCount = 0;
    }

    // ================================================================
    // 各阶段仿真算法
    // ================================================================

    /// <summary>
    /// 升温阶段：TF1 < 747°C 时线性上升
    /// </summary>
    private void HeatUp()
    {
        double increment = _config.HeatingRatePerSecond * 0.8;
        TF1 += increment + RandomNoise();
        TF2 += increment + RandomNoise();

        // 非记录阶段：TS/TC 低值跟随
        TS = TF1 * 0.3 + RandomNoise();
        TC = TF1 * 0.25 + RandomNoise();

        // 检查是否达到稳定温度
        if (TF1 >= _config.TargetFurnaceTemp - _config.StableThreshold)
        {
            // 钳位到目标温度附近，开始计数
            TF1 = _config.TargetFurnaceTemp + RandomNoise();
            TF2 = _config.TargetFurnaceTemp + RandomNoise();
            StableTickCount++;
        }
        else
        {
            StableTickCount = 0;
        }
    }

    /// <summary>
    /// 稳定阶段：钳位到 750°C
    /// </summary>
    private void MaintainFurnaceTemperature()
    {
        TF1 = _config.TargetFurnaceTemp + RandomNoise();
        TF2 = _config.TargetFurnaceTemp + RandomNoise();
        StableTickCount++;
    }

    /// <summary>
    /// 降温阶段：缓慢冷却
    /// </summary>
    private void CoolDown()
    {
        TF1 -= 0.5 + Math.Abs(RandomNoise()) * 0.1;
        TF2 -= 0.5 + Math.Abs(RandomNoise()) * 0.1;

        // 不低于室温
        if (TF1 < 25) TF1 = 25;
        if (TF2 < 25) TF2 = 25;

        TS = TF1 * 0.3 + RandomNoise();
        TC = TF1 * 0.25 + RandomNoise();
        StableTickCount = 0;
    }

    /// <summary>
    /// 非记录阶段样品温度（低值跟随）
    /// </summary>
    private void UpdateIdleSampleTemperatures()
    {
        TS = TF1 * 0.3 + RandomNoise();
        TC = TF1 * 0.25 + RandomNoise();
    }

    /// <summary>
    /// 记录阶段样品温度（指数逼近）
    /// TS 向 TF1×0.95 指数接近（系数 0.02）
    /// TC 向 TF1×0.85 指数接近（系数 0.01），比 TS 更慢
    /// </summary>
    private void UpdateRecordingSampleTemperatures(int elapsedSeconds)
    {
        double surfaceTarget = Math.Min(TF1 * 0.95, 800);
        TS += (surfaceTarget - TS) * 0.02 + RandomNoise();

        double centerTarget = Math.Min(TF1 * 0.85, 750);
        TC += (centerTarget - TC) * 0.01 + RandomNoise();
    }

    /// <summary>
    /// 记录炉温历史
    /// </summary>
    private void RecordTemperatureHistory(int seconds)
    {
        _tf1History.Enqueue((seconds, TF1));
        _tf2History.Enqueue((seconds, TF2));
        if (_tf1History.Count > 600) _tf1History.Dequeue();
        if (_tf2History.Count > 600) _tf2History.Dequeue();
    }

    /// <summary>
    /// 随机噪声：Random(-1, 1) × TempFluctuation
    /// </summary>
    private double RandomNoise()
    {
        return (_rng.NextDouble() * 2 - 1) * _config.TempFluctuation;
    }
}
