using Microsoft.Data.Sqlite;
using ISO11820.Models;

namespace ISO11820.Data;

public class DbHelper
{
    private readonly string _connectionString;

    public DbHelper(string connectionString)
    {
        _connectionString = connectionString;
        DbInitializer.Initialize(connectionString);
    }

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public Operator? ValidateLogin(string username, string password)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            "SELECT userid, username, pwd, role FROM operators WHERE username = @u AND pwd = @p", conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", password);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Operator
            {
                UserId = reader.GetString(0),
                Username = reader.GetString(1),
                Password = reader.GetString(2),
                Role = reader.GetString(3)
            };
        }
        return null;
    }

    public Apparatus? GetApparatus(string apparatusId)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            "SELECT apparatusid, apparatusname, calibrationdate, constpower, comport FROM apparatus WHERE apparatusid = @id", conn);
        cmd.Parameters.AddWithValue("@id", apparatusId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Apparatus
            {
                ApparatusId = reader.GetString(0),
                ApparatusName = reader.GetString(1),
                CalibrationDate = reader.GetString(2),
                ConstPower = reader.GetDouble(3),
                ComPort = reader.IsDBNull(4) ? "" : reader.GetString(4)
            };
        }
        return null;
    }

    public void InsertProduct(ProductMaster product)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            @"INSERT OR REPLACE INTO productmaster (productid, testid, productname, specification, height, diameter)
              VALUES (@pid, @tid, @pn, @sp, @h, @d)", conn);
        cmd.Parameters.AddWithValue("@pid", product.ProductId);
        cmd.Parameters.AddWithValue("@tid", product.TestId);
        cmd.Parameters.AddWithValue("@pn", product.ProductName);
        cmd.Parameters.AddWithValue("@sp", product.Specification);
        cmd.Parameters.AddWithValue("@h", product.Height);
        cmd.Parameters.AddWithValue("@d", product.Diameter);
        cmd.ExecuteNonQuery();
    }

    public void InsertTestMaster(TestMaster tm)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            @"INSERT INTO testmaster (productid, testid, testdate, operator, envtemp, envhumidity,
              preweight, postweight, lostweight, lostweight_per, deltatf, deltatf1, deltatf2, deltats, deltatc,
              totaltesttime, flameduration, flamestarttime, hasflame, remark, flag,
              durationmode, targetduration, apparatusid, apparatusname, apparatuscalibrationdate, constpower)
              VALUES (@pid, @tid, @td, @op, @et, @eh,
              @pw, @pow, @lw, @lwp, @dtf, @dtf1, @dtf2, @dts, @dtc,
              @ttt, @fd, @fst, @hf, @rm, @fg,
              @dm, @tdur, @aid, @aname, @acal, @cp)", conn);
        cmd.Parameters.AddWithValue("@pid", tm.ProductId);
        cmd.Parameters.AddWithValue("@tid", tm.TestId);
        cmd.Parameters.AddWithValue("@td", tm.TestDate);
        cmd.Parameters.AddWithValue("@op", tm.Operator);
        cmd.Parameters.AddWithValue("@et", tm.EnvTemp);
        cmd.Parameters.AddWithValue("@eh", tm.EnvHumidity);
        cmd.Parameters.AddWithValue("@pw", tm.PreWeight);
        cmd.Parameters.AddWithValue("@pow", tm.PostWeight);
        cmd.Parameters.AddWithValue("@lw", tm.LostWeight);
        cmd.Parameters.AddWithValue("@lwp", tm.LostWeightPer);
        cmd.Parameters.AddWithValue("@dtf", tm.DeltaTf);
        cmd.Parameters.AddWithValue("@dtf1", tm.DeltaTf1);
        cmd.Parameters.AddWithValue("@dtf2", tm.DeltaTf2);
        cmd.Parameters.AddWithValue("@dts", tm.DeltaTs);
        cmd.Parameters.AddWithValue("@dtc", tm.DeltaTc);
        cmd.Parameters.AddWithValue("@ttt", tm.TotalTestTime);
        cmd.Parameters.AddWithValue("@fd", tm.FlameDuration);
        cmd.Parameters.AddWithValue("@fst", tm.FlameStartTime);
        cmd.Parameters.AddWithValue("@hf", tm.HasFlame);
        cmd.Parameters.AddWithValue("@rm", tm.Remark);
        cmd.Parameters.AddWithValue("@fg", tm.Flag);
        cmd.Parameters.AddWithValue("@dm", tm.DurationMode);
        cmd.Parameters.AddWithValue("@tdur", tm.TargetDuration);
        cmd.Parameters.AddWithValue("@aid", tm.ApparatusId);
        cmd.Parameters.AddWithValue("@aname", tm.ApparatusName);
        cmd.Parameters.AddWithValue("@acal", tm.ApparatusCalibrationDate);
        cmd.Parameters.AddWithValue("@cp", tm.ConstPower);
        cmd.ExecuteNonQuery();
    }

    public List<TestMaster> QueryTestMasters(string? productId = null, string? operatorName = null,
        string? startDate = null, string? endDate = null)
    {
        var results = new List<TestMaster>();
        using var conn = CreateConnection();
        var sql = "SELECT * FROM testmaster WHERE 1=1";
        if (!string.IsNullOrEmpty(productId)) sql += " AND productid LIKE @pid";
        if (!string.IsNullOrEmpty(operatorName)) sql += " AND operator = @op";
        if (!string.IsNullOrEmpty(startDate)) sql += " AND testdate >= @sd";
        if (!string.IsNullOrEmpty(endDate)) sql += " AND testdate <= @ed";
        sql += " ORDER BY testdate DESC";

        using var cmd = new SqliteCommand(sql, conn);
        if (!string.IsNullOrEmpty(productId)) cmd.Parameters.AddWithValue("@pid", $"%{productId}%");
        if (!string.IsNullOrEmpty(operatorName)) cmd.Parameters.AddWithValue("@op", operatorName);
        if (!string.IsNullOrEmpty(startDate)) cmd.Parameters.AddWithValue("@sd", startDate);
        if (!string.IsNullOrEmpty(endDate)) cmd.Parameters.AddWithValue("@ed", endDate);

        using var reader = cmd.ExecuteReader();
        while (reader.Read()) results.Add(ReadTestMaster(reader));
        return results;
    }

    public TestMaster? GetTestMaster(string productId, string testId)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            "SELECT * FROM testmaster WHERE productid = @pid AND testid = @tid", conn);
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@tid", testId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadTestMaster(reader) : null;
    }

    private TestMaster ReadTestMaster(SqliteDataReader reader)
    {
        return new TestMaster
        {
            ProductId = reader.GetString(0),
            TestId = reader.GetString(1),
            TestDate = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Operator = reader.IsDBNull(3) ? "" : reader.GetString(3),
            EnvTemp = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
            EnvHumidity = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
            PreWeight = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
            PostWeight = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
            LostWeight = reader.IsDBNull(8) ? 0 : reader.GetDouble(8),
            LostWeightPer = reader.IsDBNull(9) ? 0 : reader.GetDouble(9),
            DeltaTf = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
            DeltaTf1 = reader.IsDBNull(11) ? 0 : reader.GetDouble(11),
            DeltaTf2 = reader.IsDBNull(12) ? 0 : reader.GetDouble(12),
            DeltaTs = reader.IsDBNull(13) ? 0 : reader.GetDouble(13),
            DeltaTc = reader.IsDBNull(14) ? 0 : reader.GetDouble(14),
            TotalTestTime = reader.IsDBNull(15) ? 0 : reader.GetInt32(15),
            FlameDuration = reader.IsDBNull(16) ? 0 : reader.GetInt32(16),
            FlameStartTime = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
            HasFlame = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
            Remark = reader.IsDBNull(19) ? "" : reader.GetString(19),
            Flag = reader.IsDBNull(20) ? "" : reader.GetString(20),
            DurationMode = reader.IsDBNull(21) ? "Standard" : reader.GetString(21),
            TargetDuration = reader.IsDBNull(22) ? 3600 : reader.GetInt32(22),
            ApparatusId = reader.IsDBNull(23) ? "" : reader.GetString(23),
            ApparatusName = reader.IsDBNull(24) ? "" : reader.GetString(24),
            ApparatusCalibrationDate = reader.IsDBNull(25) ? "" : reader.GetString(25),
            ConstPower = reader.IsDBNull(26) ? 0 : reader.GetDouble(26)
        };
    }

    public void InsertCalibrationRecord(CalibrationRecord cr)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            @"INSERT INTO CalibrationRecords (calibrationdate, operator, referencetemp, measuredtemp, deviation)
              VALUES (@cd, @op, @rt, @mt, @dv)", conn);
        cmd.Parameters.AddWithValue("@cd", cr.CalibrationDate);
        cmd.Parameters.AddWithValue("@op", cr.Operator);
        cmd.Parameters.AddWithValue("@rt", cr.ReferenceTemp);
        cmd.Parameters.AddWithValue("@mt", cr.MeasuredTemp);
        cmd.Parameters.AddWithValue("@dv", cr.Deviation);
        cmd.ExecuteNonQuery();
    }

    public List<CalibrationRecord> GetCalibrationRecords()
    {
        var results = new List<CalibrationRecord>();
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            "SELECT id, calibrationdate, operator, referencetemp, measuredtemp, deviation FROM CalibrationRecords ORDER BY id DESC", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new CalibrationRecord
            {
                Id = reader.GetInt32(0),
                CalibrationDate = reader.GetString(1),
                Operator = reader.GetString(2),
                ReferenceTemp = reader.GetDouble(3),
                MeasuredTemp = reader.GetDouble(4),
                Deviation = reader.GetDouble(5)
            });
        }
        return results;
    }

    public List<Operator> GetAllOperators()
    {
        var results = new List<Operator>();
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand("SELECT userid, username, pwd, role FROM operators", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new Operator
            {
                UserId = reader.GetString(0),
                Username = reader.GetString(1),
                Password = reader.GetString(2),
                Role = reader.GetString(3)
            });
        }
        return results;
    }

    public void DeleteTestMaster(string productId, string testId)
    {
        using var conn = CreateConnection();
        using var cmd = new SqliteCommand(
            "DELETE FROM testmaster WHERE productid = @pid AND testid = @tid", conn);
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@tid", testId);
        cmd.ExecuteNonQuery();
    }
}
