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

    public SensorSimulator(SimulationConfig config)
    {
        _config = config;
    }

    /// <summary>随机噪声: Random(-1, 1) × TempFluctuation (文档规格)</summary>
    private double Noise()
    {
        return (_rng.NextDouble() * 2 - 1) * _config.TempFluctuation;
    }

    /// <summary>带振幅倍率的噪声</summary>
    private double Noise(double amplitude)
    {
        return Noise() * amplitude;
    }

    public Dictionary<string, double> Update(Dictionary<string, double> current, TestState state)
    {
        var result = new Dictionary<string, double>(current);
        double tf1 = current.GetValueOrDefault("TF1", _config.InitialFurnaceTemp);
        double tf2 = current.GetValueOrDefault("TF2", _config.InitialFurnaceTemp);
        double ts  = current.GetValueOrDefault("TS", _config.InitialFurnaceTemp * 0.3);
        double tc  = current.GetValueOrDefault("TC", _config.InitialFurnaceTemp * 0.25);

        double target = _config.TargetFurnaceTemp;       // 750°C
        double threshold = _config.StableThreshold;      // 3°C
        double heatRate = _config.HeatingRatePerSecond * 0.8; // 每800ms升温量

        // === Idle: 降温阶段 ===
        if (state == TestState.Idle)
        {
            result["TF1"] = tf1 - 0.5 + Noise(0.1);
            result["TF2"] = tf2 - 0.5 + Noise(0.1);
            // TS/TC 缓慢趋向室温
            result["TS"]  = ts  + (25.0 - ts)  * 0.005 + Noise(0.1);
            result["TC"]  = tc  + (25.0 - tc)  * 0.003 + Noise(0.1);
            result["TCal"] = result["TF1"] + Noise(2.0);
            return result;
        }

        bool isRecording = (state == TestState.Recording || state == TestState.Complete);

        // === 炉温1 (TF1): 主加热通道 ===
        if (tf1 < target - threshold) // < 747°C: 升温阶段
        {
            result["TF1"] = tf1 + heatRate + Noise();
        }
        else // >= 747°C: 稳定阶段，钳位到目标温度
        {
            result["TF1"] = target + Noise();
        }

        // === 炉温2 (TF2): 副通道，与TF1独立噪声 ===
        if (tf2 < target - threshold)
        {
            result["TF2"] = tf2 + heatRate + Noise();
        }
        else
        {
            result["TF2"] = target + Noise();
        }

        // === 表面温 (TS): 非记录阶段 = TF1 × 0.3 + 噪声 ===
        if (isRecording)
        {
            double surfaceTarget = Math.Min(result["TF1"] * 0.95, 800);
            result["TS"] = ts + (surfaceTarget - ts) * 0.02 + Noise();
        }
        else
        {
            result["TS"] = result["TF1"] * 0.3 + Noise();
        }

        // === 中心温 (TC): 非记录阶段 = TF1 × 0.25 + 噪声 ===
        if (isRecording)
        {
            double centerTarget = Math.Min(result["TF1"] * 0.85, 750);
            result["TC"] = tc + (centerTarget - tc) * 0.01 + Noise();
        }
        else
        {
            result["TC"] = result["TF1"] * 0.25 + Noise();
        }

        // === 校准温 (TCal): TF1 + 噪声 × 2 ===
        result["TCal"] = result["TF1"] + Noise(2.0);

        return result;
    }

    public Dictionary<string, double> GetInitialTemperatures()
    {
        double t = _config.InitialFurnaceTemp;
        return new Dictionary<string, double>
        {
            ["TF1"]  = t,
            ["TF2"]  = t - 0.3,
            ["TS"]   = t * 0.3,
            ["TC"]   = t * 0.25,
            ["TCal"] = t + 0.5
        };
    }
}
