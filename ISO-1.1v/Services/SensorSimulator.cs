using ISO11820.Models;

namespace ISO11820.Services;

public enum TestState
{
    Idle,
    Preparing,
    Ready,
    Recording,
    Complete
}

public class SensorSimulator
{
    private readonly SimulationConfig _config;
    private readonly Random _rng = new();
    private double _phase1, _phase2, _phase3;

    public SensorSimulator(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>仿真噪声：多个随机数叠加模拟类高斯分布</summary>
    private double Noise(double amplitude = 1.0)
    {
        double n = 0;
        for (int i = 0; i < 3; i++)
            n += (_rng.NextDouble() * 2 - 1);
        return n / 3.0 * _config.TempFluctuation * amplitude;
    }

    /// <summary>缓慢变化的周期性波动（模拟电网波动等）</summary>
    private double SlowDrift(ref double phase, double period, double amplitude)
    {
        phase += 0.01 + _rng.NextDouble() * 0.005;
        return Math.Sin(phase * Math.PI * 2 / period) * amplitude;
    }

    public Dictionary<string, double> Update(Dictionary<string, double> current, TestState state)
    {
        var result = new Dictionary<string, double>(current);
        double tf1 = current.GetValueOrDefault("TF1", _config.InitialFurnaceTemp);
        double tf2 = current.GetValueOrDefault("TF2", _config.InitialFurnaceTemp);
        double ts = current.GetValueOrDefault("TS", _config.InitialFurnaceTemp * 0.95);
        double tc = current.GetValueOrDefault("TC", _config.InitialFurnaceTemp * 0.90);

        if (state == TestState.Idle)
            return result;

        bool isRecording = (state == TestState.Recording || state == TestState.Complete);
        double target = _config.TargetFurnaceTemp;
        double threshold = _config.StableThreshold;
        double heatRate = _config.HeatingRatePerSecond * 0.8;

        // === 炉温1 (TF1): 主加热通道 ===
        double slowWave = SlowDrift(ref _phase1, 20, 0.3);

        if (tf1 < target - threshold)
        {
            double progress = tf1 / target;
            double slowdown = 1.0 - progress * 0.7;
            result["TF1"] = tf1 + heatRate * Math.Max(0.2, slowdown) + Noise(1.5) + slowWave;
        }
        else
        {
            result["TF1"] = target + Noise(1.0) + slowWave;
        }

        // === 炉温2 (TF2): 副通道 ===
        double slowWave2 = SlowDrift(ref _phase2, 25, 0.25);
        if (tf2 < target - threshold)
        {
            double progress = tf2 / target;
            double slowdown = 1.0 - progress * 0.7;
            result["TF2"] = tf2 + heatRate * Math.Max(0.2, slowdown) + Noise(1.5) + slowWave2;
        }
        else
        {
            result["TF2"] = target + Noise(1.0) + slowWave2;
        }

        // === 表面温 (TS): 热惯性大，缓慢趋近炉温 ===
        if (isRecording)
        {
            double surfaceTarget = Math.Min(result["TF1"] * 0.95, 800);
            double tau = 0.02;
            result["TS"] = ts + (surfaceTarget - ts) * tau + Noise(0.5);
        }
        else
        {
            double surfaceTarget = result["TF1"] * 0.3;
            double tau = 0.02;
            result["TS"] = ts + (surfaceTarget - ts) * tau + Noise(0.6);
        }

        // === 中心温 (TC): 热惯性最大，最慢响应 ===
        if (isRecording)
        {
            double centerTarget = Math.Min(result["TF1"] * 0.85, 750);
            double tau = 0.01;
            result["TC"] = tc + (centerTarget - tc) * tau + Noise(0.4);
        }
        else
        {
            double centerTarget = result["TF1"] * 0.25;
            double tau = 0.01;
            result["TC"] = tc + (centerTarget - tc) * tau + Noise(0.5);
        }

        // === 校准温 (TCal): 接近炉温但波动更大 ===
        double slowWave3 = SlowDrift(ref _phase3, 15, 0.5);
        result["TCal"] = result["TF1"] + Noise(2.5) + slowWave3;

        return result;
    }

    public Dictionary<string, double> GetInitialTemperatures()
    {
        double t = _config.InitialFurnaceTemp;
        return new Dictionary<string, double>
        {
            ["TF1"] = t,
            ["TF2"] = t - 0.3,
            ["TS"] = t * 0.95,
            ["TC"] = t * 0.90,
            ["TCal"] = t + 0.5
        };
    }
}
