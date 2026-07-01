using ISO11820.Models;

namespace ISO11820.Data;

/// <summary>
/// 温度时序 CSV 文件管理
/// 存储路径：{BaseDirectory}\TestData\{ProductId}\{TestId}\sensor_data.csv
/// </summary>
public class SensorDataFileManager
{
    private readonly string _baseDir;

    public SensorDataFileManager(string baseDir)
    {
        _baseDir = baseDir;
    }

    /// <summary>
    /// 获取 CSV 文件的完整路径
    /// </summary>
    public string GetDataFilePath(string productId, string testId)
    {
        return Path.Combine(_baseDir, "TestData", productId, testId, "sensor_data.csv");
    }

    /// <summary>
    /// 创建数据文件和目录，写入 CSV 头
    /// </summary>
    public void CreateDataFile(string productId, string testId)
    {
        var filePath = GetDataFilePath(productId, testId);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, SensorDataPoint.CsvHeader + Environment.NewLine);
        }
    }

    /// <summary>
    /// 追加一行温度数据
    /// </summary>
    public void AppendRow(string productId, string testId, SensorDataPoint point)
    {
        var filePath = GetDataFilePath(productId, testId);
        File.AppendAllText(filePath, point.ToCsvLine() + Environment.NewLine);
    }

    /// <summary>
    /// 读取全部温度数据
    /// </summary>
    public List<SensorDataPoint> ReadAll(string productId, string testId)
    {
        var result = new List<SensorDataPoint>();
        var filePath = GetDataFilePath(productId, testId);
        if (!File.Exists(filePath)) return result;

        var lines = File.ReadAllLines(filePath);
        for (int i = 1; i < lines.Length; i++) // 跳过 header
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 6) continue;

            result.Add(new SensorDataPoint
            {
                Time = int.Parse(parts[0]),
                Temp1 = double.Parse(parts[1]),
                Temp2 = double.Parse(parts[2]),
                TempSurface = double.Parse(parts[3]),
                TempCenter = double.Parse(parts[4]),
                TempCalibration = double.Parse(parts[5])
            });
        }
        return result;
    }

    /// <summary>
    /// 检查 CSV 数据文件是否存在
    /// </summary>
    public bool DataFileExists(string productId, string testId)
    {
        return File.Exists(GetDataFilePath(productId, testId));
    }

    /// <summary>
    /// 获取文件大小（字节）
    /// </summary>
    public long GetDataFileSize(string productId, string testId)
    {
        var path = GetDataFilePath(productId, testId);
        if (!File.Exists(path)) return 0;
        return new FileInfo(path).Length;
    }
}
