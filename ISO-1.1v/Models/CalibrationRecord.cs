namespace ISO11820.Models;

public class CalibrationRecord
{
    public int Id { get; set; }
    public string CalibrationDate { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public double ReferenceTemp { get; set; }
    public double MeasuredTemp { get; set; }
    public double Deviation { get; set; }
}
