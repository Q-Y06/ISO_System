namespace ISO11820.Models;

public class TestMaster
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string TestDate { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public double EnvTemp { get; set; }
    public double EnvHumidity { get; set; }
    public double PreWeight { get; set; }
    public double PostWeight { get; set; }
    public double LostWeight { get; set; }
    public double LostWeightPer { get; set; }
    public double DeltaTf { get; set; }
    public double DeltaTf1 { get; set; }
    public double DeltaTf2 { get; set; }
    public double DeltaTs { get; set; }
    public double DeltaTc { get; set; }
    public int TotalTestTime { get; set; }
    public int FlameDuration { get; set; }
    public int FlameStartTime { get; set; }
    public int HasFlame { get; set; }
    public string Remark { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string DurationMode { get; set; } = "Standard";
    public int TargetDuration { get; set; }
    public string ApparatusId { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;
    public string ApparatusCalibrationDate { get; set; } = string.Empty;
    public double ConstPower { get; set; }
}
