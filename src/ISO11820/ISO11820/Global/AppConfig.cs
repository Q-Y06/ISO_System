namespace ISO11820.Global;

/// <summary>
/// 强类型配置类，映射 appsettings.json 结构
/// </summary>
public class AppConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public HardwareConfig Hardware { get; set; } = new();
    public SimulationConfig Simulation { get; set; } = new();
    public FileStorageConfig FileStorage { get; set; } = new();
    public ReportConfig Report { get; set; } = new();
}

public class DatabaseConfig
{
    public string Provider { get; set; } = "Sqlite";
    public string SqlitePath { get; set; } = "Data\\ISO11820.db";
}

public class HardwareConfig
{
    public int ConstPower { get; set; } = 2048;
    public int PidTemperature { get; set; } = 750;
    public string SensorProtocol { get; set; } = "ModbusRtu";
}

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
}

public class FileStorageConfig
{
    public string BaseDirectory { get; set; } = "D:\\ISO11820";
    public string TestDataDirectory { get; set; } = "D:\\ISO11820\\TestData";
}

public class ReportConfig
{
    public string OutputDirectory { get; set; } = "D:\\ISO11820\\Reports";
    public bool EnablePdfExport { get; set; } = true;
}
