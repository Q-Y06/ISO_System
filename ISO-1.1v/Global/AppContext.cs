using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Models;
using ISO11820.Services;
using Microsoft.Extensions.Configuration;

namespace ISO11820.Global;

public sealed class AppContext
{
    public static AppContext Instance { get; } = new();
    private AppContext() { }

    public IConfiguration Configuration { get; set; } = null!;
    public DbHelper Db { get; private set; } = null!;
    public SimulationConfig SimulationConfig { get; private set; } = null!;
    public SensorSimulator Simulator { get; private set; } = null!;
    public DaqWorker DaqWorker { get; private set; } = null!;
    public TestController TestController { get; private set; } = null!;
    public ExportService ExportService { get; private set; } = null!;

    public string CurrentOperator { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;

    public void Initialize()
    {
        SimulationConfig = new SimulationConfig();
        Configuration.GetSection("Simulation").Bind(SimulationConfig);

        string dbPath = Configuration["Database:SqlitePath"] ?? "Data\\ISO11820.db";
        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
        string? dbDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

        string connStr = $"Data Source={fullPath}";
        Db = new DbHelper(connStr);

        Simulator = new SensorSimulator(SimulationConfig);
        DaqWorker = new DaqWorker(Simulator, SimulationConfig);
        TestController = new TestController(Db, DaqWorker, SimulationConfig);
        ExportService = new ExportService(Configuration);

        string baseDir = Configuration["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
        string testDataDir = Configuration["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";
        string reportDir = Configuration["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";
        Directory.CreateDirectory(baseDir);
        Directory.CreateDirectory(testDataDir);
        Directory.CreateDirectory(reportDir);
    }
}
