using ISO11820.Forms;
using Microsoft.Extensions.Configuration;
using Serilog;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/iso11820-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            AppContext.Instance.Configuration = config;
            AppContext.Instance.Initialize();

            Log.Information("系统启动完成");
            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "系统启动失败");
            MessageBox.Show($"系统启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
