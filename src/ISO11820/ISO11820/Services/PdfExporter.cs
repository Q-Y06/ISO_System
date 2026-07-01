using ISO11820.Data;
using ISO11820.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Serilog;

namespace ISO11820.Services;

/// <summary>
/// PDF 导出服务 — 使用 PDFsharp-MigraDoc
/// </summary>
public class PdfExporter
{
    private readonly DbHelper _db;
    private readonly SensorDataFileManager _fileManager;
    private readonly string _outputDir;

    public PdfExporter(DbHelper db, SensorDataFileManager fileManager, string outputDir)
    {
        _db = db;
        _fileManager = fileManager;
        _outputDir = outputDir;
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    /// 导出试验报告为 PDF 文件
    /// </summary>
    public string Export(string productId, string testId, string? chartImagePath = null)
    {
        var test = _db.GetTest(productId, testId)
            ?? throw new InvalidOperationException($"试验记录不存在: {productId}/{testId}");
        var data = _fileManager.ReadAll(productId, testId);
        var outputPath = Path.Combine(_outputDir, $"{testId}_报告.pdf");

        var doc = new Document();
        doc.Info.Title = $"ISO 11820 试验报告 - {testId}";
        doc.Info.Author = test.Operator;

        // 样式
        var style = doc.Styles["Normal"]!;
        style.Font.Name = "SimSun";
        style.Font.Size = 10;

        // 标题
        var section = doc.AddSection();
        var title = section.AddParagraph("ISO 11820 建筑材料不燃性试验报告");
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.SpaceAfter = 20;

        // === 试验信息表 ===
        section.AddParagraph("一、试验信息").Format.Font.Size = 14;
        section.AddParagraph("").Format.SpaceAfter = 5;

        var infoTable = section.AddTable();
        infoTable.Borders.Width = 0.5;
        infoTable.AddColumn("4cm");
        infoTable.AddColumn("8cm");

        AddInfoRow(infoTable, "样品编号", test.ProductId);
        AddInfoRow(infoTable, "试验ID", test.TestId);
        AddInfoRow(infoTable, "试验日期", test.TestDate.ToString("yyyy-MM-dd"));
        AddInfoRow(infoTable, "操作员", test.Operator);
        AddInfoRow(infoTable, "设备名称", test.ApparatusName);
        AddInfoRow(infoTable, "环境温度", $"{test.AmbTemp:F1} °C");
        AddInfoRow(infoTable, "环境湿度", $"{test.AmbHumi:F1} %");
        AddInfoRow(infoTable, "试验前质量", $"{test.PreWeight:F2} g");
        AddInfoRow(infoTable, "试验后质量", $"{test.PostWeight:F2} g");
        AddInfoRow(infoTable, "失重量", $"{test.LostWeight:F2} g");
        AddInfoRow(infoTable, "失重率", $"{test.LostWeightPer:F2} %");
        AddInfoRow(infoTable, "试验时长", $"{test.TotalTestTime} 秒");

        // === 温度统计表 ===
        section.AddParagraph("");
        section.AddParagraph("二、温度统计").Format.Font.Size = 14;
        section.AddParagraph("").Format.SpaceAfter = 5;

        var tempTable = section.AddTable();
        tempTable.Borders.Width = 0.5;
        tempTable.AddColumn("3cm");
        tempTable.AddColumn("2.5cm");
        tempTable.AddColumn("2.5cm");
        tempTable.AddColumn("2.5cm");
        tempTable.AddColumn("2.5cm");

        var headers = new[] { "项目", "炉温1", "炉温2", "表面温度", "中心温度" };
        foreach (var h in headers)
            tempTable.AddRow().Cells[Array.IndexOf(headers, h)].AddParagraph(h).Format.Font.Bold = true;

        AddTempRow(tempTable, "最大值 (°C)", test.MaxTf1, test.MaxTf2, test.MaxTs, test.MaxTc);
        AddTempRow(tempTable, "最终值 (°C)", test.FinalTf1, test.FinalTf2, test.FinalTs, test.FinalTc);
        AddTempRow(tempTable, "温升 (°C)", test.DeltaTf1, test.DeltaTf2, test.DeltaTs, test.DeltaTc);

        // === 判定结论 ===
        section.AddParagraph("");
        section.AddParagraph("三、判定结论").Format.Font.Size = 14;
        section.AddParagraph("").Format.SpaceAfter = 5;

        bool passed = test.DeltaTf <= 50 && test.LostWeightPer <= 50 && test.FlameDuration < 5;
        var conclusion = section.AddParagraph();
        conclusion.AddText($"温升 deltatf = {test.DeltaTf:F2}°C ");
        conclusion.AddText(passed ? "≤ 50°C ✓" : "> 50°C ✗");
        conclusion.AddText($"\n失重率 = {test.LostWeightPer:F2}% ");
        conclusion.AddText(test.LostWeightPer <= 50 ? "≤ 50% ✓" : "> 50% ✗");
        conclusion.AddText($"\n火焰持续时间 = {test.FlameDuration} 秒 ");
        conclusion.AddText(test.FlameDuration < 5 ? "< 5s ✓" : "≥ 5s ✗");

        section.AddParagraph("");
        var result = section.AddParagraph($"综合判定：{(passed ? "通过" : "不通过")}");
        result.Format.Font.Size = 14;
        result.Format.Font.Bold = true;
        result.Format.Font.Color = passed ? Colors.Green : Colors.Red;

        // === 温度曲线图（如有） ===
        if (!string.IsNullOrEmpty(chartImagePath) && File.Exists(chartImagePath))
        {
            section.AddParagraph("");
            section.AddParagraph("四、温度曲线").Format.Font.Size = 14;
            section.AddParagraph("").Format.SpaceAfter = 5;
            var img = section.AddImage(chartImagePath);
            img.Width = "15cm";
            img.LockAspectRatio = true;
        }

        // === 备注 ===
        if (!string.IsNullOrEmpty(test.Memo))
        {
            section.AddParagraph("");
            section.AddParagraph("五、备注").Format.Font.Size = 14;
            section.AddParagraph(test.Memo);
        }

        // 渲染 PDF
        var renderer = new PdfDocumentRenderer();
        renderer.Document = doc;
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);

        Log.Information("PDF 导出完成: {Path}", outputPath);
        return outputPath;
    }

    private void AddInfoRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.Cells[0].AddParagraph(label).Format.Font.Bold = true;
        row.Cells[1].AddParagraph(value);
    }

    private void AddTempRow(Table table, string label, double v1, double v2, double v3, double v4)
    {
        var row = table.AddRow();
        row.Cells[0].AddParagraph(label).Format.Font.Bold = true;
        row.Cells[1].AddParagraph($"{v1:F1}");
        row.Cells[2].AddParagraph($"{v2:F1}");
        row.Cells[3].AddParagraph($"{v3:F1}");
        row.Cells[4].AddParagraph($"{v4:F1}");
    }
}
