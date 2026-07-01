namespace ISO11820.Models;

/// <summary>
/// CSV 温度时序数据的一行
/// </summary>
public class SensorDataPoint
{
    public int Time { get; set; }              // 秒数
    public double Temp1 { get; set; }          // 炉温1 (TF1)
    public double Temp2 { get; set; }          // 炉温2 (TF2)
    public double TempSurface { get; set; }    // 表面温度 (TS)
    public double TempCenter { get; set; }     // 中心温度 (TC)
    public double TempCalibration { get; set; } // 校准温度 (TCal)

    /// <summary>
    /// 转换为 CSV 行字符串
    /// </summary>
    public string ToCsvLine()
    {
        return $"{Time},{Temp1:F1},{Temp2:F1},{TempSurface:F1},{TempCenter:F1},{TempCalibration:F1}";
    }

    /// <summary>
    /// CSV 文件头
    /// </summary>
    public static string CsvHeader => "Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration";
}
