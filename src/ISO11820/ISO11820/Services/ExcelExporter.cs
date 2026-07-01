using ISO11820.Data;
using ISO11820.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using Serilog;

namespace ISO11820.Services;

/// <summary>
/// Excel 导出服务 — 使用 EPPlus 生成三 Sheet 报告
/// </summary>
public class ExcelExporter
{
    private readonly DbHelper _db;
    private readonly SensorDataFileManager _fileManager;
    private readonly string _outputDir;

    public ExcelExporter(DbHelper db, SensorDataFileManager fileManager, string outputDir)
    {
        _db = db;
        _fileManager = fileManager;
        _outputDir = outputDir;
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    /// 导出试验报告为 Excel 文件
    /// </summary>
    public string Export(string productId, string testId)
    {
        var test = _db.GetTest(productId, testId)
            ?? throw new InvalidOperationException($"试验记录不存在: {productId}/{testId}");
        var data = _fileManager.ReadAll(productId, testId);
        var outputPath = Path.Combine(_outputDir, $"{testId}_报告.xlsx");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        CreateInfoSheet(package, test, data);
        CreateDataSheet(package, data);
        CreateChartSheet(package, data);

        package.SaveAs(new FileInfo(outputPath));
        Log.Information("Excel 导出完成: {Path}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Sheet1: 试验信息表
    /// </summary>
    private void CreateInfoSheet(ExcelPackage package, TestMaster test, List<SensorDataPoint> data)
    {
        var sheet = package.Workbook.Worksheets.Add("试验信息");

        var info = new Dictionary<string, string>
        {
            ["样品编号"] = test.ProductId,
            ["试验ID"] = test.TestId,
            ["试验日期"] = test.TestDate.ToString("yyyy-MM-dd"),
            ["环境温度 (°C)"] = test.AmbTemp.ToString("F1"),
            ["环境湿度 (%)"] = test.AmbHumi.ToString("F1"),
            ["试验依据"] = test.According,
            ["操作员"] = test.Operator,
            ["设备编号"] = test.ApparatusId,
            ["设备名称"] = test.ApparatusName,
            ["试验前质量 (g)"] = test.PreWeight.ToString("F2"),
            ["试验后质量 (g)"] = test.PostWeight.ToString("F2"),
            ["失重量 (g)"] = test.LostWeight.ToString("F2"),
            ["失重率 (%)"] = test.LostWeightPer.ToString("F2"),
            ["试验时长 (秒)"] = test.TotalTestTime.ToString(),
            ["恒功率值"] = test.ConstPower.ToString(),
            ["炉温1温升 (°C)"] = test.DeltaTf1.ToString("F2"),
            ["炉温2温升 (°C)"] = test.DeltaTf2.ToString("F2"),
            ["表面温升 (°C)"] = test.DeltaTs.ToString("F2"),
            ["中心温升 (°C)"] = test.DeltaTc.ToString("F2"),
            ["综合温升 (°C)"] = test.DeltaTf.ToString("F2"),
            ["现象编码"] = test.PhenoCode,
            ["火焰发生时刻 (秒)"] = test.FlameTime.ToString(),
            ["火焰持续时间 (秒)"] = test.FlameDuration.ToString(),
            ["备注"] = test.Memo ?? "",
        };

        sheet.Cells["A1"].Value = "ISO 11820 试验报告";
        sheet.Cells["A1"].Style.Font.Size = 16;
        sheet.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        foreach (var kv in info)
        {
            sheet.Cells[row, 1].Value = kv.Key;
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 2].Value = kv.Value;
            row++;
        }

        // 判定结论
        row += 1;
        sheet.Cells[row, 1].Value = "判定结论";
        sheet.Cells[row, 1].Style.Font.Bold = true;
        sheet.Cells[row, 1].Style.Font.Size = 14;

        bool passed = test.DeltaTf <= 50 && test.LostWeightPer <= 50 && test.FlameDuration < 5;
        sheet.Cells[row, 2].Value = passed ? "通过" : "不通过";
        sheet.Cells[row, 2].Style.Font.Color.SetColor(passed ? System.Drawing.Color.Green : System.Drawing.Color.Red);

        sheet.Column(1).Width = 25;
        sheet.Column(2).Width = 25;
    }

    /// <summary>
    /// Sheet2: 温度数据表
    /// </summary>
    private void CreateDataSheet(ExcelPackage package, List<SensorDataPoint> data)
    {
        var sheet = package.Workbook.Worksheets.Add("温度数据");
        sheet.Cells[1, 1].Value = "时间 (秒)";
        sheet.Cells[1, 2].Value = "炉温1 (°C)";
        sheet.Cells[1, 3].Value = "炉温2 (°C)";
        sheet.Cells[1, 4].Value = "表面温度 (°C)";
        sheet.Cells[1, 5].Value = "中心温度 (°C)";
        sheet.Cells[1, 6].Value = "校准温度 (°C)";

        for (int i = 1; i <= 6; i++)
            sheet.Cells[1, i].Style.Font.Bold = true;

        for (int i = 0; i < data.Count; i++)
        {
            var p = data[i];
            sheet.Cells[i + 2, 1].Value = p.Time;
            sheet.Cells[i + 2, 2].Value = p.Temp1;
            sheet.Cells[i + 2, 3].Value = p.Temp2;
            sheet.Cells[i + 2, 4].Value = p.TempSurface;
            sheet.Cells[i + 2, 5].Value = p.TempCenter;
            sheet.Cells[i + 2, 6].Value = p.TempCalibration;
        }

        sheet.Column(1).Width = 12;
        for (int i = 2; i <= 6; i++)
            sheet.Column(i).Width = 16;
    }

    /// <summary>
    /// Sheet3: 温度曲线图
    /// </summary>
    private void CreateChartSheet(ExcelPackage package, List<SensorDataPoint> data)
    {
        var sheet = package.Workbook.Worksheets.Add("温度曲线");

        // 图表的源数据需要放在某个 sheet 中，这里将数据复制到曲线 sheet
        sheet.Cells[1, 1].Value = "时间";
        sheet.Cells[1, 2].Value = "炉温1";
        sheet.Cells[1, 3].Value = "炉温2";
        sheet.Cells[1, 4].Value = "表面温度";
        sheet.Cells[1, 5].Value = "中心温度";

        for (int i = 0; i < data.Count; i++)
        {
            var p = data[i];
            sheet.Cells[i + 2, 1].Value = p.Time;
            sheet.Cells[i + 2, 2].Value = p.Temp1;
            sheet.Cells[i + 2, 3].Value = p.Temp2;
            sheet.Cells[i + 2, 4].Value = p.TempSurface;
            sheet.Cells[i + 2, 5].Value = p.TempCenter;
        }

        int rowCount = data.Count + 1;

        var chart = (ExcelLineChart)sheet.Drawings.AddChart("TemperatureChart", eChartType.Line);
        chart.Title.Text = "温度曲线";
        chart.SetPosition(2, 0, 7, 0);
        chart.SetSize(800, 500);

        // X 轴
        var xRange = sheet.Cells[2, 1, rowCount, 1];

        AddSeries(chart, sheet, "炉温1", 2, rowCount, 2);
        AddSeries(chart, sheet, "炉温2", 3, rowCount, 3);
        AddSeries(chart, sheet, "表面温度", 4, rowCount, 4);
        AddSeries(chart, sheet, "中心温度", 5, rowCount, 5);

        chart.XAxis.Title.Text = "时间 (秒)";
        chart.YAxis.Title.Text = "温度 (°C)";
    }

    private void AddSeries(ExcelLineChart chart, ExcelWorksheet sheet,
                           string name, int fromCol, int toRow, int dataCol)
    {
        var series = chart.Series.Add(
            sheet.Cells[2, dataCol, toRow, dataCol],
            sheet.Cells[2, fromCol, toRow, fromCol]);
        series.Header = name;
    }
}
