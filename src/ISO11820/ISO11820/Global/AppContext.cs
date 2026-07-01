using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Models;
using ISO11820.Services;
using ISO11820.Services.Simulation;
using ISO11820.Utilities;
using Microsoft.Extensions.Configuration;

namespace ISO11820.Global;

/// <summary>
/// 全局应用上下文 — 单例，持有所有核心对象
/// </summary>
public class AppContext
{
    private static readonly Lazy<AppContext> _instance = new(() => new AppContext());
    public static AppContext Instance => _instance.Value;

    // ===== 配置 =====
    public AppConfig Config { get; private set; } = null!;

    // ===== 当前用户 =====
    public Operator? CurrentUser { get; set; }

    // ===== 数据层 =====
    public DbHelper Db { get; private set; } = null!;

    // ===== 核心 =====
    public TestController TestController { get; private set; } = null!;
    public SensorSimulator Simulator { get; private set; } = null!;

    // ===== 服务 =====
    public DaqWorker DaqWorker { get; private set; } = null!;
    public SensorDataFileManager DataFileManager { get; private set; } = null!;
    public ExportService ExportService { get; private set; } = null!;
    public CalibrationService CalibrationService { get; private set; } = null!;

    private AppContext()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 1. 加载配置
        Config = LoadConfiguration();

        // 2. 初始化 Serilog
        LogHelper.Initialize(Config.FileStorage.BaseDirectory);

        // 3. 初始化数据库
        var dbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            Config.Database.SqlitePath);
        Db = new DbHelper(dbPath);
        DbInitializer.Initialize(Db, dbPath);

        // 4. 初始化仿真引擎
        Simulator = new SensorSimulator(Config.Simulation);

        // 5. 初始化文件管理
        DataFileManager = new SensorDataFileManager(Config.FileStorage.BaseDirectory);

        // 6. 初始化试验控制器（状态机）
        TestController = new TestController(Db, Simulator, DataFileManager, Config);

        // 7. 初始化 DAQ 工作线程
        DaqWorker = new DaqWorker(TestController);

        // 8. 初始化导出服务
        ExportService = new ExportService(Db, DataFileManager, Config.Report.OutputDirectory);

        // 9. 初始化校准服务
        CalibrationService = new CalibrationService(Db, Simulator);
    }

    /// <summary>
    /// 启动 DAQ 采集循环
    /// </summary>
    public void StartDaq()
    {
        DaqWorker.Start();
    }

    /// <summary>
    /// 停止 DAQ 采集循环
    /// </summary>
    public void StopDaq()
    {
        DaqWorker.Stop();
    }

    /// <summary>
    /// 关闭应用时清理资源
    /// </summary>
    public void Shutdown()
    {
        StopDaq();
        DaqWorker.Dispose();
        LogHelper.Close();
    }

    private AppConfig LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        var configuration = builder.Build();

        var config = new AppConfig();
        configuration.Bind(config);
        return config;
    }
}
