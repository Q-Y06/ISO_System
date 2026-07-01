using ISO11820.Data;
using ISO11820.Models;
using Serilog;

namespace ISO11820.Services;

/// <summary>
/// CSV 导出服务
/// </summary>
public class CsvExporter
{
    private readonly SensorDataFileManager _fileManager;
    private readonly string _outputDir;

    public CsvExporter(SensorDataFileManager fileManager, string outputDir)
    {
        _fileManager = fileManager;
        _outputDir = outputDir;
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    /// 导出试验数据为 CSV 文件
    /// </summary>
    public string Export(string productId, string testId)
    {
        var data = _fileManager.ReadAll(productId, testId);
        var outputPath = Path.Combine(_outputDir, $"{testId}_data.csv");

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        writer.WriteLine(SensorDataPoint.CsvHeader);
        foreach (var point in data)
            writer.WriteLine(point.ToCsvLine());

        Log.Information("CSV 导出完成: {Path}, 共 {Count} 行", outputPath, data.Count);
        return outputPath;
    }
}
