namespace ISO11820.Global;

/// <summary>
/// 应用程序常量
/// </summary>
public static class AppConstants
{
    /// <summary>试验完成标记值</summary>
    public const string TestCompleteFlag = "10000000";

    /// <summary>目标炉温 (°C)</summary>
    public const double TargetFurnaceTemp = 750.0;

    /// <summary>稳定温度下限 (°C)</summary>
    public const double StableTempLower = 745.0;

    /// <summary>稳定温度上限 (°C)</summary>
    public const double StableTempUpper = 755.0;

    /// <summary>标准试验时长（秒）</summary>
    public const int StandardTestDuration = 3600;

    /// <summary>DAQ 采集周期（毫秒）</summary>
    public const int DaqIntervalMs = 800;

    /// <summary>温漂计算窗口（数据点数）</summary>
    public const int DriftWindowSize = 600;

    /// <summary>温漂阈值（°C/10min）</summary>
    public const double MaxDriftPer10Min = 2.0;

    /// <summary>样品通过判定：温升上限 (°C)</summary>
    public const double PassDeltaTfMax = 50.0;

    /// <summary>样品通过判定：失重率上限 (%)</summary>
    public const double PassLostWeightPerMax = 50.0;

    /// <summary>样品通过判定：火焰持续最大（秒）</summary>
    public const int PassFlameDurationMax = 5;
}
