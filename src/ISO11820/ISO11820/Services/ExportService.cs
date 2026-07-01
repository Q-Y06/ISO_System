using ISO11820.Data;
using Serilog;

namespace ISO11820.Services;

/// <summary>
/// 导出服务门面 — 协调 CSV/Excel/PDF 三种导出
/// </summary>
public class ExportService
{
    private readonly DbHelper _db;
    private readonly SensorDataFileManager _fileManager;
    private readonly CsvExporter _csv;
    private readonly ExcelExporter _excel;
    private readonly PdfExporter _pdf;

    public ExportService(DbHelper db, SensorDataFileManager fileManager,
                         string outputDir)
    {
        _db = db;
        _fileManager = fileManager;
        OutputDirectory = outputDir;
        _csv = new CsvExporter(fileManager, outputDir);
        _excel = new ExcelExporter(db, fileManager, outputDir);
        _pdf = new PdfExporter(db, fileManager, outputDir);
    }

    /// <summary>
    /// 导出 CSV
    /// </summary>
    public string ExportCsv(string productId, string testId)
    {
        Log.Information("开始导出 CSV: {ProductId}/{TestId}", productId, testId);
        return _csv.Export(productId, testId);
    }

    /// <summary>
    /// 导出 Excel
    /// </summary>
    public string ExportExcel(string productId, string testId)
    {
        Log.Information("开始导出 Excel: {ProductId}/{TestId}", productId, testId);
        return _excel.Export(productId, testId);
    }

    /// <summary>
    /// 导出 PDF（可选嵌入图表图片）
    /// </summary>
    public string ExportPdf(string productId, string testId, string? chartImagePath = null)
    {
        Log.Information("开始导出 PDF: {ProductId}/{TestId}", productId, testId);
        return _pdf.Export(productId, testId, chartImagePath);
    }

    /// <summary>
    /// 试验完成后自动导出所有格式
    /// </summary>
    public (string Csv, string Excel, string Pdf) ExportAll(string productId, string testId,
                                                              string? chartImagePath = null)
    {
        var csv = ExportCsv(productId, testId);
        var excel = ExportExcel(productId, testId);
        var pdf = ExportPdf(productId, testId, chartImagePath);
        return (csv, excel, pdf);
    }

    /// <summary>
    /// 获取导出目录
    /// </summary>
    public string OutputDirectory { get; private set; }
}
