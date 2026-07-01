using Serilog;
using Serilog.Events;

namespace ISO11820.Utilities;

/// <summary>
/// Serilog 日志初始化
/// </summary>
public static class LogHelper
{
    public static void Initialize(string baseDir)
    {
        var logDir = Path.Combine(baseDir, "Logs");
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.File(
                Path.Combine(logDir, "iso11820-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== ISO 11820 系统启动 ===");
    }

    public static void Close()
    {
        Log.Information("=== ISO 11820 系统关闭 ===");
        Log.CloseAndFlush();
    }
}
