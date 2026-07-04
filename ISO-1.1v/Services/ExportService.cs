using System.Text;
using ISO11820.Models;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace ISO11820.Services;

public class ExportService
{
    private readonly IConfiguration _config;

    public ExportService(IConfiguration config)
    {
        _config = config;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private string BaseDir => _config["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
    private string TestDataDir => _config["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";
    private string ReportDir => _config["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";

    public string ExportCsv(TestMaster tm, List<TemperatureData> tempData)
    {
        string dir = Path.Combine(TestDataDir, tm.ProductId, tm.TestId);
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, "sensor_data.csv");
        var sb = new StringBuilder();
        sb.AppendLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        foreach (var td in tempData)
            sb.AppendLine($"{td.Time},{td.Temp1:F1},{td.Temp2:F1},{td.TempSurface:F1},{td.TempCenter:F1},{td.TempCalibration:F1}");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public string ExportExcel(TestMaster tm, List<TemperatureData> tempData)
    {
        Directory.CreateDirectory(ReportDir);
        string filePath = Path.Combine(ReportDir, $"{tm.TestId}_报告.xlsx");

        using var package = new ExcelPackage();
        var sheet1 = package.Workbook.Worksheets.Add("试验信息");
        sheet1.Cells["A1"].Value = "ISO 11820 不燃性试验报告";
        sheet1.Cells["A1"].Style.Font.Size = 16;
        sheet1.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        WriteInfoRow(sheet1, ref row, "样品编号", tm.ProductId);
        WriteInfoRow(sheet1, ref row, "试验标识", tm.TestId);
        WriteInfoRow(sheet1, ref row, "试验日期", tm.TestDate);
        WriteInfoRow(sheet1, ref row, "操作员", tm.Operator);
        WriteInfoRow(sheet1, ref row, "环境温度 (°C)", $"{tm.EnvTemp:F1}");
        WriteInfoRow(sheet1, ref row, "环境湿度 (%)", $"{tm.EnvHumidity:F1}");
        WriteInfoRow(sheet1, ref row, "试验前质量 (g)", $"{tm.PreWeight:F2}");
        WriteInfoRow(sheet1, ref row, "试验后质量 (g)", $"{tm.PostWeight:F2}");
        WriteInfoRow(sheet1, ref row, "失重率 (%)", $"{tm.LostWeightPer:F2}");
        WriteInfoRow(sheet1, ref row, "炉温1温升 (°C)", $"{tm.DeltaTf1:F1}");
        WriteInfoRow(sheet1, ref row, "炉温2温升 (°C)", $"{tm.DeltaTf2:F1}");
        WriteInfoRow(sheet1, ref row, "表面温升 (°C)", $"{tm.DeltaTs:F1}");
        WriteInfoRow(sheet1, ref row, "中心温升 (°C)", $"{tm.DeltaTc:F1}");
        WriteInfoRow(sheet1, ref row, "综合温升 (°C)", $"{tm.DeltaTf:F1}");
        WriteInfoRow(sheet1, ref row, "火焰持续时间 (s)", $"{tm.FlameDuration}");
        WriteInfoRow(sheet1, ref row, "总试验时长 (s)", $"{tm.TotalTestTime}");

        row++;
        string verdict = (tm.DeltaTf <= 50 && tm.LostWeightPer <= 50 && tm.FlameDuration < 5)
            ? "通过 — 材料判定为不燃" : "不通过";
        sheet1.Cells[$"A{row}"].Value = $"判定结论: {verdict}";
        sheet1.Cells[$"A{row}"].Style.Font.Bold = true;
        sheet1.Column(1).Width = 25;
        sheet1.Column(2).Width = 25;

        var sheet2 = package.Workbook.Worksheets.Add("温度数据");
        sheet2.Cells["A1"].Value = "Time (s)";
        sheet2.Cells["B1"].Value = "炉温1 (°C)";
        sheet2.Cells["C1"].Value = "炉温2 (°C)";
        sheet2.Cells["D1"].Value = "表面温 (°C)";
        sheet2.Cells["E1"].Value = "中心温 (°C)";
        sheet2.Cells["F1"].Value = "校准温 (°C)";
        for (int i = 0; i < tempData.Count; i++)
        {
            var td = tempData[i];
            int r = i + 2;
            sheet2.Cells[$"A{r}"].Value = td.Time;
            sheet2.Cells[$"B{r}"].Value = td.Temp1;
            sheet2.Cells[$"C{r}"].Value = td.Temp2;
            sheet2.Cells[$"D{r}"].Value = td.TempSurface;
            sheet2.Cells[$"E{r}"].Value = td.TempCenter;
            sheet2.Cells[$"F{r}"].Value = td.TempCalibration;
        }

        // Sheet3: chart
        var sheet3 = package.Workbook.Worksheets.Add("温度曲线");
        if (tempData.Count > 0)
        {
            // Copy data to sheet3 for chart source
            sheet3.Cells["A1"].Value = "Time";
            sheet3.Cells["B1"].Value = "炉温1";
            sheet3.Cells["C1"].Value = "炉温2";
            sheet3.Cells["D1"].Value = "表面温";
            sheet3.Cells["E1"].Value = "中心温";
            for (int i = 0; i < tempData.Count; i++)
            {
                int r = i + 2;
                sheet3.Cells[$"A{r}"].Value = tempData[i].Time;
                sheet3.Cells[$"B{r}"].Value = tempData[i].Temp1;
                sheet3.Cells[$"C{r}"].Value = tempData[i].Temp2;
                sheet3.Cells[$"D{r}"].Value = tempData[i].TempSurface;
                sheet3.Cells[$"E{r}"].Value = tempData[i].TempCenter;
            }
            var chart = sheet3.Drawings.AddChart("TempChart", OfficeOpenXml.Drawing.Chart.eChartType.XYScatterLines);
            chart.Title.Text = "温度曲线";
            chart.SetPosition(0, 0, 6, 0);
            chart.SetSize(800, 500);
            for (int col = 1; col <= 4; col++)
            {
                var ser = chart.Series.Add(
                    sheet3.Cells[2, col + 1, tempData.Count + 1, col + 1],
                    sheet3.Cells[2, 1, tempData.Count + 1, 1]);
                ser.Header = col switch { 1 => "炉温1", 2 => "炉温2", 3 => "表面温", 4 => "中心温", _ => "" };
            }
        }

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    private void WriteInfoRow(ExcelWorksheet sheet, ref int row, string label, string value)
    {
        sheet.Cells[$"A{row}"].Value = label;
        sheet.Cells[$"B{row}"].Value = value;
        row++;
    }

    public string ExportPdf(TestMaster tm)
    {
        Directory.CreateDirectory(ReportDir);
        string filePath = Path.Combine(ReportDir, $"{tm.TestId}_报告.pdf");

        using var document = new PdfDocument();
        document.Info.Title = $"ISO 11820 试验报告 - {tm.TestId}";
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var fontTitle = new XFont("Microsoft YaHei", 16, XFontStyleEx.Bold);
        var fontNormal = new XFont("Microsoft YaHei", 11, XFontStyleEx.Regular);
        var fontBold = new XFont("Microsoft YaHei", 11, XFontStyleEx.Bold);

        double pageW = page.Width.Point;
        double y = 40;
        gfx.DrawString("ISO 11820 不燃性试验报告", fontTitle, XBrushes.Black,
            new XRect(XUnit.FromPoint(0), XUnit.FromPoint(y), page.Width, XUnit.FromPoint(30)), XStringFormats.TopCenter);
        y += 40;

        DrawLine(gfx, ref y, "样品编号", tm.ProductId, fontNormal, pageW);
        DrawLine(gfx, ref y, "试验标识", tm.TestId, fontNormal, pageW);
        DrawLine(gfx, ref y, "试验日期", tm.TestDate, fontNormal, pageW);
        DrawLine(gfx, ref y, "操作员", tm.Operator, fontNormal, pageW);
        DrawLine(gfx, ref y, "失重率", $"{tm.LostWeightPer:F2}%", fontNormal, pageW);
        DrawLine(gfx, ref y, "综合温升", $"{tm.DeltaTf:F1} °C", fontNormal, pageW);
        DrawLine(gfx, ref y, "试验时长", $"{tm.TotalTestTime} 秒", fontNormal, pageW);
        y += 20;

        string verdict = (tm.DeltaTf <= 50 && tm.LostWeightPer <= 50 && tm.FlameDuration < 5)
            ? "判定结论: 通过 — 材料判定为不燃" : "判定结论: 不通过";
        gfx.DrawString(verdict, fontBold, XBrushes.Black,
            new XRect(XUnit.FromPoint(50), XUnit.FromPoint(y), page.Width - XUnit.FromPoint(100), XUnit.FromPoint(25)), XStringFormats.TopLeft);

        document.Save(filePath);
        return filePath;
    }

    private void DrawLine(XGraphics gfx, ref double y, string label, string value, XFont font, double pageWidth)
    {
        gfx.DrawString($"{label}: {value}", font, XBrushes.Black,
            new XRect(XUnit.FromPoint(50), XUnit.FromPoint(y), XUnit.FromPoint(pageWidth - 100), XUnit.FromPoint(22)), XStringFormats.TopLeft);
        y += 22;
    }
}
