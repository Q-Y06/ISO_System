using Microsoft.Data.Sqlite;

namespace ISO11820.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var commands = new[]
        {
            @"CREATE TABLE IF NOT EXISTS operators (
                userid TEXT PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                pwd TEXT NOT NULL,
                role TEXT NOT NULL
            )",
            @"CREATE TABLE IF NOT EXISTS productmaster (
                productid TEXT NOT NULL,
                testid TEXT NOT NULL,
                productname TEXT,
                specification TEXT,
                height REAL,
                diameter REAL,
                PRIMARY KEY (productid, testid)
            )",
            @"CREATE TABLE IF NOT EXISTS apparatus (
                apparatusid TEXT PRIMARY KEY,
                apparatusname TEXT,
                calibrationdate TEXT,
                constpower REAL,
                comport TEXT
            )",
            @"CREATE TABLE IF NOT EXISTS sensors (
                sensorid TEXT PRIMARY KEY,
                channelid INTEGER,
                sensorname TEXT,
                rangehigh REAL,
                rangelow REAL
            )",
            @"CREATE TABLE IF NOT EXISTS testmaster (
                productid TEXT NOT NULL,
                testid TEXT NOT NULL,
                testdate TEXT,
                operator TEXT,
                envtemp REAL,
                envhumidity REAL,
                preweight REAL,
                postweight REAL,
                lostweight REAL,
                lostweight_per REAL,
                deltatf REAL,
                deltatf1 REAL,
                deltatf2 REAL,
                deltats REAL,
                deltatc REAL,
                totaltesttime INTEGER,
                flameduration INTEGER,
                flamestarttime INTEGER,
                hasflame INTEGER DEFAULT 0,
                remark TEXT,
                flag TEXT,
                durationmode TEXT DEFAULT 'Standard',
                targetduration INTEGER DEFAULT 3600,
                apparatusid TEXT,
                apparatusname TEXT,
                apparatuscalibrationdate TEXT,
                constpower REAL,
                PRIMARY KEY (productid, testid)
            )",
            @"CREATE TABLE IF NOT EXISTS CalibrationRecords (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                calibrationdate TEXT,
                operator TEXT,
                referencetemp REAL,
                measuredtemp REAL,
                deviation REAL
            )"
        };

        foreach (var sql in commands)
        {
            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        SeedOperators(conn);
        SeedApparatus(conn);
        SeedSensors(conn);
    }

    private static void SeedOperators(SqliteConnection conn)
    {
        using var check = new SqliteCommand("SELECT COUNT(*) FROM operators", conn);
        if ((long)check.ExecuteScalar()! > 0) return;
        using var cmd = new SqliteCommand(
            @"INSERT INTO operators (userid, username, pwd, role) VALUES
              ('001', 'admin', '123456', 'admin'),
              ('002', 'experimenter', '123456', 'experimenter')", conn);
        cmd.ExecuteNonQuery();
    }

    private static void SeedApparatus(SqliteConnection conn)
    {
        using var check = new SqliteCommand("SELECT COUNT(*) FROM apparatus", conn);
        if ((long)check.ExecuteScalar()! > 0) return;
        using var cmd = new SqliteCommand(
            @"INSERT INTO apparatus (apparatusid, apparatusname, calibrationdate, constpower, comport)
              VALUES ('ISO11820-001', '不燃性试验炉', '2026-01-01', 2048, 'COM1')", conn);
        cmd.ExecuteNonQuery();
    }

    private static void SeedSensors(SqliteConnection conn)
    {
        using var check = new SqliteCommand("SELECT COUNT(*) FROM sensors", conn);
        if ((long)check.ExecuteScalar()! > 0) return;
        using var cmd = new SqliteCommand(
            @"INSERT INTO sensors (sensorid, channelid, sensorname, rangehigh, rangelow) VALUES
              ('TF1', 1, '炉温1', 1000, 0),
              ('TF2', 2, '炉温2', 1000, 0),
              ('TS',  3, '表面温', 1000, 0),
              ('TC',  4, '中心温', 1000, 0),
              ('TCal',5, '校准温', 1000, 0)", conn);
        cmd.ExecuteNonQuery();
    }
}
