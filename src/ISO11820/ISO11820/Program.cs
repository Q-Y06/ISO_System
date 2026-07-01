using ISO11820.Global;
using ISO11820.UI.Forms;
using OfficeOpenXml;
using Serilog;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820;

static class Program
{
    /// <summary>
    /// ISO 11820 建筑材料不燃性试验仿真系统 — 入口点
    /// </summary>
    [STAThread]
    static void Main()
    {
        // EPPlus 许可证（非商业用途）
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // WinForms 高DPI初始化
        ApplicationConfiguration.Initialize();

        try
        {
            // 初始化全局 Application Context（单例，包含所有核心对象）
            var ctx = AppContext.Instance;
            Log.Information("应用初始化完成");

            // 启动登录窗体
            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序异常终止");
            MessageBox.Show($"程序发生严重错误:\n{ex.Message}\n\n详细信息请查看日志文件。",
                "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            AppContext.Instance.Shutdown();
        }
    }
}
