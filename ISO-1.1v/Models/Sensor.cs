namespace ISO11820.Models;

public class Sensor
{
    public string SensorId { get; set; } = string.Empty;
    public int ChannelId { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public double RangeHigh { get; set; }
    public double RangeLow { get; set; }
}
