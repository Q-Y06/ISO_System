using Microsoft.Data.Sqlite;
using ISO11820.Models;

namespace ISO11820.Data;

/// <summary>
/// SQLite 数据库操作封装类
/// 所有数据库操作通过此类完成，使用参数化查询
/// </summary>
public class DbHelper
{
    private readonly string _connStr;

    public DbHelper(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        return conn;
    }

    // ================================================================
    // 登录验证
    // ================================================================

    /// <summary>
    /// 验证登录（按 username + pwd 查询）
    /// </summary>
    public bool ValidateLogin(string username, string pwd, out string userId, out string userType)
    {
        userId = ""; userType = "";
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, usertype FROM operators WHERE username=$name AND pwd=$pwd";
        cmd.Parameters.AddWithValue("$name", username);
        cmd.Parameters.AddWithValue("$pwd", pwd);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            userId = reader.GetString(0);
            userType = reader.GetString(1);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有操作员
    /// </summary>
    public List<Operator> GetAllOperators()
    {
        var result = new List<Operator>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, username, pwd, usertype FROM operators";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Operator
            {
                UserId = reader.GetString(0),
                UserName = reader.GetString(1),
                Pwd = reader.GetString(2),
                UserType = reader.GetString(3)
            });
        }
        return result;
    }

    // ================================================================
    // 设备操作
    // ================================================================

    public Apparatus? GetApparatus(int apparatusId = 0)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower FROM apparatus WHERE apparatusid=$id";
        cmd.Parameters.AddWithValue("$id", apparatusId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Apparatus
            {
                ApparatusId = reader.GetInt32(0),
                InnerNumber = reader.GetString(1),
                ApparatusName = reader.GetString(2),
                CheckDateF = DateTime.Parse(reader.GetString(3)),
                CheckDateT = DateTime.Parse(reader.GetString(4)),
                PidPort = reader.GetString(5),
                PowerPort = reader.GetString(6),
                ConstPower = !reader.IsDBNull(7) ? reader.GetInt32(7) : null
            };
        }
        return null;
    }

    public void UpdateConstPower(int apparatusId, int constPower)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE apparatus SET constpower=$power WHERE apparatusid=$id";
        cmd.Parameters.AddWithValue("$power", constPower);
        cmd.Parameters.AddWithValue("$id", apparatusId);
        cmd.ExecuteNonQuery();
    }

    // ================================================================
    // 样品操作
    // ================================================================

    public bool ProductExists(string productId)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM productmaster WHERE productid=$pid";
        cmd.Parameters.AddWithValue("$pid", productId);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    public void InsertProduct(ProductMaster product)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO productmaster (productid, productname, specific, diameter, height, flag)
                           VALUES ($pid, $name, $spec, $dia, $height, $flag)";
        cmd.Parameters.AddWithValue("$pid", product.ProductId);
        cmd.Parameters.AddWithValue("$name", product.ProductName);
        cmd.Parameters.AddWithValue("$spec", product.Specific);
        cmd.Parameters.AddWithValue("$dia", product.Diameter);
        cmd.Parameters.AddWithValue("$height", product.Height);
        cmd.Parameters.AddWithValue("$flag", (object?)product.Flag ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public ProductMaster? GetProduct(string productId)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT productid, productname, specific, diameter, height, flag FROM productmaster WHERE productid=$pid";
        cmd.Parameters.AddWithValue("$pid", productId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new ProductMaster
            {
                ProductId = reader.GetString(0),
                ProductName = reader.GetString(1),
                Specific = reader.GetString(2),
                Diameter = reader.GetDouble(3),
                Height = reader.GetDouble(4),
                Flag = !reader.IsDBNull(5) ? reader.GetString(5) : null
            };
        }
        return null;
    }

    // ================================================================
    // 试验操作（核心）
    // ================================================================

    /// <summary>
    /// 新建试验（初始插入，统计字段填0）
    /// </summary>
    public void InsertTest(TestMaster test)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO testmaster
                (productid, testid, testdate, operator, ambtemp, ambhumi,
                 according, apparatusid, apparatusname, apparatuschkdate, rptno,
                 preweight, postweight, lostweight, lostweight_per,
                 totaltesttime, constpower, phenocode, flametime, flameduration,
                 maxtf1, maxtf2, maxts, maxtc,
                 maxtf1_time, maxtf2_time, maxts_time, maxtc_time,
                 finaltf1, finaltf2, finalts, finaltc,
                 finaltf1_time, finaltf2_time, finalts_time, finaltc_time,
                 deltatf1, deltatf2, deltatf, deltats, deltatc)
            VALUES
                ($pid, $tid, $date, $op, $ambtemp, $ambhumi,
                 $according, $appid, $appname, $appchk, $rptno,
                 $prewt, 0, 0, 0,
                 0, $constpower, '', 0, 0,
                 0, 0, 0, 0, 0, 0, 0, 0,
                 0, 0, 0, 0, 0, 0, 0, 0,
                 0, 0, 0, 0, 0)";
        cmd.Parameters.AddWithValue("$pid", test.ProductId);
        cmd.Parameters.AddWithValue("$tid", test.TestId);
        cmd.Parameters.AddWithValue("$date", test.TestDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$op", test.Operator);
        cmd.Parameters.AddWithValue("$ambtemp", test.AmbTemp);
        cmd.Parameters.AddWithValue("$ambhumi", test.AmbHumi);
        cmd.Parameters.AddWithValue("$according", test.According);
        cmd.Parameters.AddWithValue("$appid", test.ApparatusId);
        cmd.Parameters.AddWithValue("$appname", test.ApparatusName);
        cmd.Parameters.AddWithValue("$appchk", test.ApparatusChkDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$rptno", test.RptNo);
        cmd.Parameters.AddWithValue("$prewt", test.PreWeight);
        cmd.Parameters.AddWithValue("$constpower", test.ConstPower);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 试验完成后更新统计字段
    /// </summary>
    public void UpdateTestResult(TestMaster test)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE testmaster SET
                postweight      = $post,
                lostweight      = $lost,
                lostweight_per  = $lostper,
                totaltesttime   = $time,
                constpower      = $constpower,
                phenocode       = $pheno,
                flametime       = $ftime,
                flameduration   = $fdur,
                maxtf1 = $maxtf1, maxtf2 = $maxtf2, maxts = $maxts, maxtc = $maxtc,
                maxtf1_time = $maxtf1t, maxtf2_time = $maxtf2t, maxts_time = $maxtst, maxtc_time = $matct,
                finaltf1 = $ftf1, finaltf2 = $ftf2, finalts = $fts, finaltc = $ftc,
                finaltf1_time = $ftf1t, finaltf2_time = $ftf2t, finalts_time = $ftst, finaltc_time = $ftct,
                deltatf1 = $dtf1, deltatf2 = $dtf2, deltatf = $dtf, deltats = $dts, deltatc = $dtc,
                memo = $memo,
                flag = '10000000'
            WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$post", test.PostWeight);
        cmd.Parameters.AddWithValue("$lost", test.LostWeight);
        cmd.Parameters.AddWithValue("$lostper", test.LostWeightPer);
        cmd.Parameters.AddWithValue("$time", test.TotalTestTime);
        cmd.Parameters.AddWithValue("$constpower", test.ConstPower);
        cmd.Parameters.AddWithValue("$pheno", test.PhenoCode);
        cmd.Parameters.AddWithValue("$ftime", test.FlameTime);
        cmd.Parameters.AddWithValue("$fdur", test.FlameDuration);
        cmd.Parameters.AddWithValue("$maxtf1", test.MaxTf1);
        cmd.Parameters.AddWithValue("$maxtf2", test.MaxTf2);
        cmd.Parameters.AddWithValue("$maxts", test.MaxTs);
        cmd.Parameters.AddWithValue("$maxtc", test.MaxTc);
        cmd.Parameters.AddWithValue("$maxtf1t", test.MaxTf1Time);
        cmd.Parameters.AddWithValue("$maxtf2t", test.MaxTf2Time);
        cmd.Parameters.AddWithValue("$maxtst", test.MaxTsTime);
        cmd.Parameters.AddWithValue("$matct", test.MaxTcTime);
        cmd.Parameters.AddWithValue("$ftf1", test.FinalTf1);
        cmd.Parameters.AddWithValue("$ftf2", test.FinalTf2);
        cmd.Parameters.AddWithValue("$fts", test.FinalTs);
        cmd.Parameters.AddWithValue("$ftc", test.FinalTc);
        cmd.Parameters.AddWithValue("$ftf1t", test.FinalTf1Time);
        cmd.Parameters.AddWithValue("$ftf2t", test.FinalTf2Time);
        cmd.Parameters.AddWithValue("$ftst", test.FinalTsTime);
        cmd.Parameters.AddWithValue("$ftct", test.FinalTcTime);
        cmd.Parameters.AddWithValue("$dtf1", test.DeltaTf1);
        cmd.Parameters.AddWithValue("$dtf2", test.DeltaTf2);
        cmd.Parameters.AddWithValue("$dtf", test.DeltaTf);
        cmd.Parameters.AddWithValue("$dts", test.DeltaTs);
        cmd.Parameters.AddWithValue("$dtc", test.DeltaTc);
        cmd.Parameters.AddWithValue("$memo", (object?)test.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pid", test.ProductId);
        cmd.Parameters.AddWithValue("$tid", test.TestId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 获取单条试验记录
    /// </summary>
    public TestMaster? GetTest(string productId, string testId)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMaster(reader);
        return null;
    }

    /// <summary>
    /// 查询试验历史列表
    /// </summary>
    public List<TestMaster> QueryTests(DateTime from, DateTime to, string? productId = null, string? operatorName = null)
    {
        var result = new List<TestMaster>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();

        var conditions = new List<string> { "testdate BETWEEN $from AND $to" };
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));

        if (!string.IsNullOrEmpty(productId))
        {
            conditions.Add("productid LIKE '%' || $pid || '%'");
            cmd.Parameters.AddWithValue("$pid", productId);
        }
        if (!string.IsNullOrEmpty(operatorName))
        {
            conditions.Add("operator = $op");
            cmd.Parameters.AddWithValue("$op", operatorName);
        }

        cmd.CommandText = $"SELECT * FROM testmaster WHERE {string.Join(" AND ", conditions)} ORDER BY testdate DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadTestMaster(reader));
        return result;
    }

    /// <summary>
    /// 检查是否有已完成但未保存的试验
    /// </summary>
    public bool HasUnsavedCompletedTest()
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM testmaster WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000')";
        return (long)cmd.ExecuteScalar()! > 0;
    }

    /// <summary>
    /// 获取当前活动但未保存的试验
    /// </summary>
    public TestMaster? GetActiveUnsavedTest()
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000') LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMaster(reader);
        return null;
    }

    // ================================================================
    // 传感器操作
    // ================================================================

    public List<Sensor> GetAllSensors()
    {
        var result = new List<Sensor>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM sensors ORDER BY sensorid";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Sensor
            {
                SensorId = reader.GetInt32(0),
                SensorName = reader.GetString(1),
                DispName = reader.GetString(2),
                SensorGroup = reader.GetString(3),
                Unit = reader.GetString(4),
                Discription = reader.GetString(5),
                Flag = reader.GetString(6),
                SignalZero = reader.GetDouble(7),
                SignalSpan = reader.GetDouble(8),
                OutputZero = reader.GetDouble(9),
                OutputSpan = reader.GetDouble(10),
                OutputValue = reader.GetDouble(11),
                InputValue = reader.GetDouble(12),
                SignalType = reader.GetInt32(13)
            });
        }
        return result;
    }

    public void UpdateSensorValue(int sensorId, double outputValue, double inputValue)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE sensors SET outputvalue=$ov, inputvalue=$iv WHERE sensorid=$id";
        cmd.Parameters.AddWithValue("$ov", outputValue);
        cmd.Parameters.AddWithValue("$iv", inputValue);
        cmd.Parameters.AddWithValue("$id", sensorId);
        cmd.ExecuteNonQuery();
    }

    // ================================================================
    // 校准记录操作
    // ================================================================

    public void InsertCalibration(CalibrationRecord record)
    {
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO CalibrationRecords
            (Id, CalibrationDate, CalibrationType, ApparatusId, Operator,
             TemperatureData, UniformityResult, MaxDeviation, AverageTemperature,
             PassedCriteria, Remarks, CreatedAt,
             TempA1, TempA2, TempA3, TempB1, TempB2, TempB3, TempC1, TempC2, TempC3,
             TAvg, TAvgAxis1, TAvgAxis2, TAvgAxis3, TAvgLevela, TAvgLevelb, TAvgLevelc,
             TDevAxis1, TDevAxis2, TDevAxis3, TDevLevela, TDevLevelb, TDevLevelc,
             TAvgDevAxis, TAvgDevLevel, CenterTempData, Memo)
            VALUES
            ($id, $date, $type, $appid, $op,
             $tempData, $uniResult, $maxDev, $avgTemp,
             $passed, $remarks, $created,
             $tA1, $tA2, $tA3, $tB1, $tB2, $tB3, $tC1, $tC2, $tC3,
             $tAvg, $tAvgAx1, $tAvgAx2, $tAvgAx3, $tAvgLa, $tAvgLb, $tAvgLc,
             $tDevAx1, $tDevAx2, $tDevAx3, $tDevLa, $tDevLb, $tDevLc,
             $tAvgDevAx, $tAvgDevLev, $centerData, $memo)";
        cmd.Parameters.AddWithValue("$id", record.Id);
        cmd.Parameters.AddWithValue("$date", record.CalibrationDate);
        cmd.Parameters.AddWithValue("$type", record.CalibrationType);
        cmd.Parameters.AddWithValue("$appid", record.ApparatusId);
        cmd.Parameters.AddWithValue("$op", record.Operator);
        cmd.Parameters.AddWithValue("$tempData", record.TemperatureData);
        cmd.Parameters.AddWithValue("$uniResult", (object?)record.UniformityResult ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$maxDev", (object?)record.MaxDeviation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$avgTemp", (object?)record.AverageTemperature ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$passed", record.PassedCriteria);
        cmd.Parameters.AddWithValue("$remarks", record.Remarks);
        cmd.Parameters.AddWithValue("$created", record.CreatedAt);
        cmd.Parameters.AddWithValue("$tA1", (object?)record.TempA1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tA2", (object?)record.TempA2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tA3", (object?)record.TempA3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tB1", (object?)record.TempB1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tB2", (object?)record.TempB2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tB3", (object?)record.TempB3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tC1", (object?)record.TempC1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tC2", (object?)record.TempC2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tC3", (object?)record.TempC3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvg", (object?)record.TAvg ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgAx1", (object?)record.TAvgAxis1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgAx2", (object?)record.TAvgAxis2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgAx3", (object?)record.TAvgAxis3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgLa", (object?)record.TAvgLevela ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgLb", (object?)record.TAvgLevelb ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgLc", (object?)record.TAvgLevelc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tDevAx1", (object?)record.TDevAxis1 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tDevAx2", (object?)record.TDevAxis2 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tDevAx3", (object?)record.TDevAxis3 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tDevLa", (object?)record.TDevLevela ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tDevLb", (object?)record.TDevLevelb ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tDevLc", (object?)record.TDevLevelc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgDevAx", (object?)record.TAvgDevAxis ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tAvgDevLev", (object?)record.TAvgDevLevel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$centerData", (object?)record.CenterTempData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", (object?)record.Memo ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<CalibrationRecord> GetCalibrations(int? apparatusId = null, string? calibrationType = null)
    {
        var result = new List<CalibrationRecord>();
        using var conn = OpenConnection();
        var cmd = conn.CreateCommand();

        var conditions = new List<string>();
        if (apparatusId.HasValue)
        {
            conditions.Add("ApparatusId=$appid");
            cmd.Parameters.AddWithValue("$appid", apparatusId.Value);
        }
        if (!string.IsNullOrEmpty(calibrationType))
        {
            conditions.Add("CalibrationType=$type");
            cmd.Parameters.AddWithValue("$type", calibrationType);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        cmd.CommandText = $"SELECT * FROM CalibrationRecords {where} ORDER BY CalibrationDate DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadCalibrationRecord(reader));
        return result;
    }

    // ================================================================
    // 辅助方法：从 DataReader 读取对象
    // ================================================================

    private TestMaster ReadTestMaster(SqliteDataReader reader)
    {
        return new TestMaster
        {
            ProductId = reader.GetString(reader.GetOrdinal("productid")),
            TestId = reader.GetString(reader.GetOrdinal("testid")),
            TestDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("testdate"))),
            AmbTemp = reader.GetDouble(reader.GetOrdinal("ambtemp")),
            AmbHumi = reader.GetDouble(reader.GetOrdinal("ambhumi")),
            According = reader.GetString(reader.GetOrdinal("according")),
            Operator = reader.GetString(reader.GetOrdinal("operator")),
            ApparatusId = reader.GetString(reader.GetOrdinal("apparatusid")),
            ApparatusName = reader.GetString(reader.GetOrdinal("apparatusname")),
            ApparatusChkDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("apparatuschkdate"))),
            RptNo = reader.GetString(reader.GetOrdinal("rptno")),
            PreWeight = reader.GetDouble(reader.GetOrdinal("preweight")),
            PostWeight = reader.GetDouble(reader.GetOrdinal("postweight")),
            LostWeight = reader.GetDouble(reader.GetOrdinal("lostweight")),
            LostWeightPer = reader.GetDouble(reader.GetOrdinal("lostweight_per")),
            TotalTestTime = reader.GetInt32(reader.GetOrdinal("totaltesttime")),
            ConstPower = reader.GetInt32(reader.GetOrdinal("constpower")),
            PhenoCode = reader.GetString(reader.GetOrdinal("phenocode")),
            FlameTime = reader.GetInt32(reader.GetOrdinal("flametime")),
            FlameDuration = reader.GetInt32(reader.GetOrdinal("flameduration")),
            MaxTf1 = reader.GetDouble(reader.GetOrdinal("maxtf1")),
            MaxTf2 = reader.GetDouble(reader.GetOrdinal("maxtf2")),
            MaxTs = reader.GetDouble(reader.GetOrdinal("maxts")),
            MaxTc = reader.GetDouble(reader.GetOrdinal("maxtc")),
            MaxTf1Time = reader.GetInt32(reader.GetOrdinal("maxtf1_time")),
            MaxTf2Time = reader.GetInt32(reader.GetOrdinal("maxtf2_time")),
            MaxTsTime = reader.GetInt32(reader.GetOrdinal("maxts_time")),
            MaxTcTime = reader.GetInt32(reader.GetOrdinal("maxtc_time")),
            FinalTf1 = reader.GetDouble(reader.GetOrdinal("finaltf1")),
            FinalTf2 = reader.GetDouble(reader.GetOrdinal("finaltf2")),
            FinalTs = reader.GetDouble(reader.GetOrdinal("finalts")),
            FinalTc = reader.GetDouble(reader.GetOrdinal("finaltc")),
            FinalTf1Time = reader.GetInt32(reader.GetOrdinal("finaltf1_time")),
            FinalTf2Time = reader.GetInt32(reader.GetOrdinal("finaltf2_time")),
            FinalTsTime = reader.GetInt32(reader.GetOrdinal("finalts_time")),
            FinalTcTime = reader.GetInt32(reader.GetOrdinal("finaltc_time")),
            DeltaTf1 = reader.GetDouble(reader.GetOrdinal("deltatf1")),
            DeltaTf2 = reader.GetDouble(reader.GetOrdinal("deltatf2")),
            DeltaTf = reader.GetDouble(reader.GetOrdinal("deltatf")),
            DeltaTs = reader.GetDouble(reader.GetOrdinal("deltats")),
            DeltaTc = reader.GetDouble(reader.GetOrdinal("deltatc")),
            Memo = !reader.IsDBNull(reader.GetOrdinal("memo")) ? reader.GetString(reader.GetOrdinal("memo")) : null,
            Flag = !reader.IsDBNull(reader.GetOrdinal("flag")) ? reader.GetString(reader.GetOrdinal("flag")) : null
        };
    }

    private CalibrationRecord ReadCalibrationRecord(SqliteDataReader reader)
    {
        double? ReadNullableDouble(string col) =>
            !reader.IsDBNull(reader.GetOrdinal(col)) ? reader.GetDouble(reader.GetOrdinal(col)) : null;
        string? ReadNullableString(string col) =>
            !reader.IsDBNull(reader.GetOrdinal(col)) ? reader.GetString(reader.GetOrdinal(col)) : null;

        return new CalibrationRecord
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            CalibrationDate = reader.GetString(reader.GetOrdinal("CalibrationDate")),
            CalibrationType = reader.GetString(reader.GetOrdinal("CalibrationType")),
            ApparatusId = reader.GetInt32(reader.GetOrdinal("ApparatusId")),
            Operator = reader.GetString(reader.GetOrdinal("Operator")),
            TemperatureData = reader.GetString(reader.GetOrdinal("TemperatureData")),
            UniformityResult = ReadNullableDouble("UniformityResult"),
            MaxDeviation = ReadNullableDouble("MaxDeviation"),
            AverageTemperature = ReadNullableDouble("AverageTemperature"),
            PassedCriteria = reader.GetInt32(reader.GetOrdinal("PassedCriteria")),
            Remarks = reader.GetString(reader.GetOrdinal("Remarks")),
            CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt")),
            TempA1 = ReadNullableDouble("TempA1"),
            TempA2 = ReadNullableDouble("TempA2"),
            TempA3 = ReadNullableDouble("TempA3"),
            TempB1 = ReadNullableDouble("TempB1"),
            TempB2 = ReadNullableDouble("TempB2"),
            TempB3 = ReadNullableDouble("TempB3"),
            TempC1 = ReadNullableDouble("TempC1"),
            TempC2 = ReadNullableDouble("TempC2"),
            TempC3 = ReadNullableDouble("TempC3"),
            TAvg = ReadNullableDouble("TAvg"),
            TAvgAxis1 = ReadNullableDouble("TAvgAxis1"),
            TAvgAxis2 = ReadNullableDouble("TAvgAxis2"),
            TAvgAxis3 = ReadNullableDouble("TAvgAxis3"),
            TAvgLevela = ReadNullableDouble("TAvgLevela"),
            TAvgLevelb = ReadNullableDouble("TAvgLevelb"),
            TAvgLevelc = ReadNullableDouble("TAvgLevelc"),
            TDevAxis1 = ReadNullableDouble("TDevAxis1"),
            TDevAxis2 = ReadNullableDouble("TDevAxis2"),
            TDevAxis3 = ReadNullableDouble("TDevAxis3"),
            TDevLevela = ReadNullableDouble("TDevLevela"),
            TDevLevelb = ReadNullableDouble("TDevLevelb"),
            TDevLevelc = ReadNullableDouble("TDevLevelc"),
            TAvgDevAxis = ReadNullableDouble("TAvgDevAxis"),
            TAvgDevLevel = ReadNullableDouble("TAvgDevLevel"),
            CenterTempData = ReadNullableString("CenterTempData"),
            Memo = ReadNullableString("Memo")
        };
    }
}
