namespace ISO11820.Models;

/// <summary>
/// 传感器配置模型 — 对应 sensors 表
/// </summary>
public class Sensor
{
    public int SensorId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public string DispName { get; set; } = string.Empty;
    public string SensorGroup { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Discription { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public double SignalZero { get; set; }
    public double SignalSpan { get; set; }
    public double OutputZero { get; set; }
    public double OutputSpan { get; set; }
    public double OutputValue { get; set; }
    public double InputValue { get; set; }
    public int SignalType { get; set; }
}
