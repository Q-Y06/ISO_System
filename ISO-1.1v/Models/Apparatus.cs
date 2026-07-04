namespace ISO11820.Models;

public class Apparatus
{
    public string ApparatusId { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;
    public string CalibrationDate { get; set; } = string.Empty;
    public double ConstPower { get; set; }
    public string ComPort { get; set; } = string.Empty;
}
