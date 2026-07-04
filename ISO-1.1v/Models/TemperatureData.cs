namespace ISO11820.Models;

public class TemperatureData
{
    public int Time { get; set; }
    public double Temp1 { get; set; }
    public double Temp2 { get; set; }
    public double TempSurface { get; set; }
    public double TempCenter { get; set; }
    public double TempCalibration { get; set; }
}
