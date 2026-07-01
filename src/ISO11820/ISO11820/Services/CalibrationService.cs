using System.Text.Json;
using ISO11820.Data;
using ISO11820.Models;
using ISO11820.Services.Simulation;
using Serilog;

namespace ISO11820.Services;

/// <summary>
/// 设备校准服务
/// </summary>
public class CalibrationService
{
    private readonly DbHelper _db;
    private readonly SensorSimulator _simulator;

    public CalibrationService(DbHelper db, SensorSimulator simulator)
    {
        _db = db;
        _simulator = simulator;
    }

    /// <summary>
    /// 执行一次表面温度校准
    /// </summary>
    public CalibrationRecord PerformSurfaceCalibration(int apparatusId, string operatorName)
    {
        // 读取9个测温点（仿真生成）
        var temps = new double[3, 3]; // [层A/B/C, 轴1/2/3]
        for (int layer = 0; layer < 3; layer++)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                // 仿真：在 TF1 基础上加小幅偏差
                double baseNoise = (new Random().NextDouble() * 2 - 1) * 5.0;
                double layerOffset = (layer - 1) * 3.0;  // 中间层高，上下层略低
                double axisOffset = (axis - 1) * 2.0;
                temps[layer, axis] = _simulator.TF1 + baseNoise + layerOffset + axisOffset;
            }
        }

        // 计算均匀性指标
        var allTemps = temps.Cast<double>().ToList();
        double tAvg = allTemps.Average();
        double maxDev = allTemps.Max(t => Math.Abs(t - tAvg));

        // 各轴平均
        double avgAxis1 = (temps[0, 0] + temps[1, 0] + temps[2, 0]) / 3;
        double avgAxis2 = (temps[0, 1] + temps[1, 1] + temps[2, 1]) / 3;
        double avgAxis3 = (temps[0, 2] + temps[1, 2] + temps[2, 2]) / 3;

        // 各层平均
        double avgLevelA = (temps[0, 0] + temps[0, 1] + temps[0, 2]) / 3;
        double avgLevelB = (temps[1, 0] + temps[1, 1] + temps[1, 2]) / 3;
        double avgLevelC = (temps[2, 0] + temps[2, 1] + temps[2, 2]) / 3;

        // 各轴偏差
        double devAxis1 = avgAxis1 - tAvg;
        double devAxis2 = avgAxis2 - tAvg;
        double devAxis3 = avgAxis3 - tAvg;

        // 各层偏差
        double devLevelA = avgLevelA - tAvg;
        double devLevelB = avgLevelB - tAvg;
        double devLevelC = avgLevelC - tAvg;

        // 平均轴偏差和平均层偏差
        double avgDevAxis = (Math.Abs(devAxis1) + Math.Abs(devAxis2) + Math.Abs(devAxis3)) / 3;
        double avgDevLevel = (Math.Abs(devLevelA) + Math.Abs(devLevelB) + Math.Abs(devLevelC)) / 3;

        // 均匀性结果（使用最大偏差）
        double uniformityResult = Math.Max(avgDevAxis, avgDevLevel);

        // 判断是否通过（最大偏差 < 10°C 为通过）
        int passed = maxDev < 10.0 ? 1 : 0;

        var record = new CalibrationRecord
        {
            Id = Guid.NewGuid().ToString(),
            CalibrationDate = DateTime.Now.ToString("O"),
            CalibrationType = "Surface",
            ApparatusId = apparatusId,
            Operator = operatorName,
            TemperatureData = JsonSerializer.Serialize(new
            {
                temperatures = temps,
                timestamp = DateTime.Now.ToString("O")
            }),
            UniformityResult = Math.Round(uniformityResult, 2),
            MaxDeviation = Math.Round(maxDev, 2),
            AverageTemperature = Math.Round(tAvg, 1),
            PassedCriteria = passed,
            Remarks = passed == 1 ? "校准通过" : "校准未通过：最大偏差超标",
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

            TempA1 = Math.Round(temps[0, 0], 1),
            TempA2 = Math.Round(temps[0, 1], 1),
            TempA3 = Math.Round(temps[0, 2], 1),
            TempB1 = Math.Round(temps[1, 0], 1),
            TempB2 = Math.Round(temps[1, 1], 1),
            TempB3 = Math.Round(temps[1, 2], 1),
            TempC1 = Math.Round(temps[2, 0], 1),
            TempC2 = Math.Round(temps[2, 1], 1),
            TempC3 = Math.Round(temps[2, 2], 1),

            TAvg = Math.Round(tAvg, 1),
            TAvgAxis1 = Math.Round(avgAxis1, 1),
            TAvgAxis2 = Math.Round(avgAxis2, 1),
            TAvgAxis3 = Math.Round(avgAxis3, 1),
            TAvgLevela = Math.Round(avgLevelA, 1),
            TAvgLevelb = Math.Round(avgLevelB, 1),
            TAvgLevelc = Math.Round(avgLevelC, 1),
            TDevAxis1 = Math.Round(devAxis1, 2),
            TDevAxis2 = Math.Round(devAxis2, 2),
            TDevAxis3 = Math.Round(devAxis3, 2),
            TDevLevela = Math.Round(devLevelA, 2),
            TDevLevelb = Math.Round(devLevelB, 2),
            TDevLevelc = Math.Round(devLevelC, 2),
            TAvgDevAxis = Math.Round(avgDevAxis, 2),
            TAvgDevLevel = Math.Round(avgDevLevel, 2)
        };

        _db.InsertCalibration(record);
        Log.Information("校准完成: Id={Id}, Passed={P}, MaxDev={D}", record.Id, passed, maxDev);

        return record;
    }

    /// <summary>
    /// 获取校准历史
    /// </summary>
    public List<CalibrationRecord> GetCalibrationHistory(int? apparatusId = null)
    {
        return _db.GetCalibrations(apparatusId);
    }

    /// <summary>
    /// 获取最新的校准记录
    /// </summary>
    public CalibrationRecord? GetLatestCalibration(string type = "Surface")
    {
        var records = _db.GetCalibrations(null, type);
        return records.FirstOrDefault();
    }
}
