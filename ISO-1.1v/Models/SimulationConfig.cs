namespace ISO11820.Models;

public class SimulationConfig
{
    public bool EnableSimulation { get; set; } = true;
    public bool SimulateSensors { get; set; } = true;
    public bool SimulatePidController { get; set; } = true;
    public double InitialFurnaceTemp { get; set; } = 720.0;
    public double TargetFurnaceTemp { get; set; } = 750.0;
    public double HeatingRatePerSecond { get; set; } = 40.0;
    public double TempFluctuation { get; set; } = 0.5;
    public double StableThreshold { get; set; } = 3.0;
    public bool SimulateFlame { get; set; } = false;
    public double MaxTemperatureDriftPerTenMinutes { get; set; } = 2.0;
}
