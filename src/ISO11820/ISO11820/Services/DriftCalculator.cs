using MathNet.Numerics;

namespace ISO11820.Services;

/// <summary>
/// 温度漂移计算 — 使用 MathNet.Numerics 线性回归
/// </summary>
public static class DriftCalculator
{
    /// <summary>
    /// 计算温度的 10 分钟漂移率
    /// </summary>
    /// <param name="dataPoints">(时间秒, 温度) 序列</param>
    /// <returns>漂移率 °C/10min，数据不足时返回 0</returns>
    public static double CalculateDrift(List<(double Time, double Temp)> dataPoints)
    {
        if (dataPoints.Count < 30) return 0;

        var times = dataPoints.Select(p => p.Time).ToArray();
        var temps = dataPoints.Select(p => p.Temp).ToArray();

        try
        {
            var (intercept, slope) = Fit.Line(times, temps);
            // 斜率单位是 °C/s，乘以 600 转换为 °C/10min
            return slope * 600;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 判断两个通道的温漂是否都在阈值内
    /// </summary>
    public static bool IsWithinDriftLimit(double driftTF1, double driftTF2, double maxDrift = 2.0)
    {
        return Math.Abs(driftTF1) <= maxDrift && Math.Abs(driftTF2) <= maxDrift;
    }
}
