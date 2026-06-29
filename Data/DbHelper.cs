using Microsoft.Data.Sqlite;
using ISO11820.Models;
using System.Text.Json;

namespace ISO11820.Data;

/// <summary>
/// SQLite 数据库操作封装
/// </summary>
public class DbHelper
{
    private readonly string _connStr;

    public DbHelper(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        return conn;
    }

    // ==================== 数据库初始化 ====================

    public void InitializeDatabase()
    {
        using var conn = CreateConnection();

        // 创建 operators 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS operators (
                userid   TEXT NOT NULL,
                username TEXT NOT NULL,
                pwd      TEXT NOT NULL,
                usertype TEXT NOT NULL
            );");

        // 创建 apparatus 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS apparatus (
                apparatusid   INTEGER NOT NULL PRIMARY KEY,
                innernumber   TEXT NOT NULL,
                apparatusname TEXT NOT NULL,
                checkdatef    date NOT NULL,
                checkdatet    date NOT NULL,
                pidport       TEXT NOT NULL,
                powerport     TEXT NOT NULL,
                constpower    INTEGER NULL
            );");

        // 创建 productmaster 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS productmaster (
                productid   TEXT NOT NULL PRIMARY KEY,
                productname TEXT NOT NULL,
                specific    TEXT NOT NULL,
                diameter    REAL NOT NULL,
                height      REAL NOT NULL,
                flag        TEXT NULL
            );");

        // 创建 testmaster 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS testmaster (
                productid        TEXT NOT NULL,
                testid           TEXT NOT NULL,
                testdate         date NOT NULL,
                ambtemp          REAL NOT NULL,
                ambhumi          REAL NOT NULL,
                according        TEXT NOT NULL,
                operator         TEXT NOT NULL,
                apparatusid      TEXT NOT NULL,
                apparatusname    TEXT NOT NULL,
                apparatuschkdate date NOT NULL,
                rptno            TEXT NOT NULL,
                preweight        REAL NOT NULL,
                postweight       REAL NOT NULL,
                lostweight       REAL NOT NULL,
                lostweight_per   REAL NOT NULL,
                totaltesttime    INTEGER NOT NULL,
                constpower       INTEGER NOT NULL,
                phenocode        TEXT NOT NULL,
                flametime        INTEGER NOT NULL,
                flameduration    INTEGER NOT NULL,
                maxtf1           REAL NOT NULL,
                maxtf2           REAL NOT NULL,
                maxts            REAL NOT NULL,
                maxtc            REAL NOT NULL,
                maxtf1_time      INTEGER NOT NULL,
                maxtf2_time      INTEGER NOT NULL,
                maxts_time       INTEGER NOT NULL,
                maxtc_time       INTEGER NOT NULL,
                finaltf1         REAL NOT NULL,
                finaltf2         REAL NOT NULL,
                finalts          REAL NOT NULL,
                finaltc          REAL NOT NULL,
                finaltf1_time    INTEGER NOT NULL,
                finaltf2_time    INTEGER NOT NULL,
                finalts_time     INTEGER NOT NULL,
                finaltc_time     INTEGER NOT NULL,
                deltatf1         REAL NOT NULL,
                deltatf2         REAL NOT NULL,
                deltatf          REAL NOT NULL,
                deltats          REAL NOT NULL,
                deltatc          REAL NOT NULL,
                memo             TEXT NULL,
                flag             TEXT NULL,
                PRIMARY KEY (productid, testid),
                FOREIGN KEY (productid) REFERENCES productmaster(productid)
            );");

        // 创建索引
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate ON testmaster (testdate);");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_Testmaster_Operator ON testmaster (operator);");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate_Productid ON testmaster (testdate, productid);");

        // 创建 sensors 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS sensors (
                sensorid    INTEGER NOT NULL PRIMARY KEY,
                sensorname  TEXT NOT NULL,
                dispname    TEXT NOT NULL,
                sensorgroup TEXT NOT NULL,
                unit        TEXT NOT NULL,
                discription TEXT NOT NULL,
                flag        TEXT NOT NULL,
                signalzero  REAL NOT NULL,
                signalspan  REAL NOT NULL,
                outputzero  REAL NOT NULL,
                outputspan  REAL NOT NULL,
                outputvalue REAL NOT NULL,
                inputvalue  REAL NOT NULL,
                signaltype  INTEGER NOT NULL
            );");

        // 创建 CalibrationRecords 表
        ExecuteNonQuery(conn, @"
            CREATE TABLE IF NOT EXISTS CalibrationRecords (
                Id                 TEXT NOT NULL PRIMARY KEY,
                CalibrationDate    TEXT NOT NULL,
                CalibrationType    TEXT NOT NULL,
                ApparatusId        INTEGER NOT NULL,
                Operator           TEXT NOT NULL,
                TemperatureData    TEXT NOT NULL,
                UniformityResult   REAL NULL,
                MaxDeviation       REAL NULL,
                AverageTemperature REAL NULL,
                PassedCriteria     INTEGER NOT NULL,
                Remarks            TEXT NOT NULL,
                CreatedAt          TEXT NOT NULL,
                TempA1 REAL NULL, TempA2 REAL NULL, TempA3 REAL NULL,
                TempB1 REAL NULL, TempB2 REAL NULL, TempB3 REAL NULL,
                TempC1 REAL NULL, TempC2 REAL NULL, TempC3 REAL NULL,
                TAvg        REAL NULL,
                TAvgAxis1   REAL NULL, TAvgAxis2 REAL NULL, TAvgAxis3 REAL NULL,
                TAvgLevela  REAL NULL, TAvgLevelb REAL NULL, TAvgLevelc REAL NULL,
                TDevAxis1   REAL NULL, TDevAxis2 REAL NULL, TDevAxis3 REAL NULL,
                TDevLevela  REAL NULL, TDevLevelb REAL NULL, TDevLevelc REAL NULL,
                TAvgDevAxis REAL NULL, TAvgDevLevel REAL NULL,
                CenterTempData TEXT NULL,
                Memo           TEXT NULL
            );");

        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Date ON CalibrationRecords (CalibrationDate);");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Operator ON CalibrationRecords (Operator);");

        // 插入初始数据
        SeedInitialData(conn);
    }

    private void SeedInitialData(SqliteConnection conn)
    {
        // 操作员
        ExecuteNonQuery(conn, @"
            INSERT INTO operators (userid, username, pwd, usertype)
            SELECT '1', 'admin', '123456', 'admin'
            WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin');");

        ExecuteNonQuery(conn, @"
            INSERT INTO operators (userid, username, pwd, usertype)
            SELECT '2', 'experimenter', '123456', 'operator'
            WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter');");

        // 设备
        ExecuteNonQuery(conn, @"
            INSERT INTO apparatus (apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower)
            SELECT 0, 'FURNACE-01', '一号试验炉', date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048
            WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid = 0);");

        // 传感器（5个业务通道）
        EnsureSensor(conn, 0, "Sensor0", "炉温1", "采集", "℃", "炉温1");
        EnsureSensor(conn, 1, "Sensor1", "炉温2", "采集", "℃", "炉温2");
        EnsureSensor(conn, 2, "Sensor2", "表面温度", "采集", "℃", "表面温度");
        EnsureSensor(conn, 3, "Sensor3", "中心温度", "采集", "℃", "中心温度");
        EnsureSensor(conn, 16, "Sensor16", "校准温度", "校准", "℃", "校准温度");

        // 备用通道 4~15
        for (int i = 4; i <= 15; i++)
        {
            EnsureSensor(conn, i, $"Sensor{i}", $"备用通道{i + 1}", "备用", "℃", $"备用通道{i + 1}");
        }
    }

    private void EnsureSensor(SqliteConnection conn, int id, string name, string dispName, string group, string unit, string desc)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO sensors (sensorid, sensorname, dispname, sensorgroup, unit, discription, flag, signalzero, signalspan, outputzero, outputspan, outputvalue, inputvalue, signaltype)
            SELECT $id, $name, $disp, $grp, $unit, $desc, '启用', 0, 0, 0, 1000, 0, 0, 4
            WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid = $id);";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$disp", dispName);
        cmd.Parameters.AddWithValue("$grp", group);
        cmd.Parameters.AddWithValue("$unit", unit);
        cmd.Parameters.AddWithValue("$desc", desc);
        cmd.ExecuteNonQuery();
    }

    private void ExecuteNonQuery(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // ==================== 登录 ====================

    public bool Login(string username, string pwd, out string userid, out string usertype)
    {
        userid = "";
        usertype = "";
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, usertype FROM operators WHERE username=$name AND pwd=$pwd";
        cmd.Parameters.AddWithValue("$name", username);
        cmd.Parameters.AddWithValue("$pwd", pwd);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            userid = reader.GetString(0);
            usertype = reader.GetString(1);
            return true;
        }
        return false;
    }

    // ==================== 样品 ====================

    public void InsertProduct(ProductMaster product)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO productmaster (productid, productname, specific, diameter, height, flag)
            VALUES ($pid, $name, $spec, $dia, $h, $flag)";
        cmd.Parameters.AddWithValue("$pid", product.ProductId);
        cmd.Parameters.AddWithValue("$name", product.ProductName);
        cmd.Parameters.AddWithValue("$spec", product.Specific);
        cmd.Parameters.AddWithValue("$dia", product.Diameter);
        cmd.Parameters.AddWithValue("$h", product.Height);
        cmd.Parameters.AddWithValue("$flag", product.Flag ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public ProductMaster? GetProduct(string productId)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM productmaster WHERE productid=$pid";
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
                Flag = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }
        return null;
    }

    // ==================== 设备 ====================

    public Apparatus? GetApparatus()
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM apparatus LIMIT 1";
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
                ConstPower = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            };
        }
        return null;
    }

    // ==================== 传感器 ====================

    public List<Sensor> GetSensors()
    {
        var list = new List<Sensor>();
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM sensors ORDER BY sensorid";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Sensor
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
        return list;
    }

    public void UpdateSensorValue(int sensorId, double outputValue, double inputValue)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE sensors SET outputvalue=$ov, inputvalue=$iv WHERE sensorid=$id";
        cmd.Parameters.AddWithValue("$ov", outputValue);
        cmd.Parameters.AddWithValue("$iv", inputValue);
        cmd.Parameters.AddWithValue("$id", sensorId);
        cmd.ExecuteNonQuery();
    }

    // ==================== 试验记录 ====================

    public void InsertTestMaster(TestMasterRecord record)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO testmaster
                (productid, testid, testdate, ambtemp, ambhumi, according, operator,
                 apparatusid, apparatusname, apparatuschkdate, rptno,
                 preweight, postweight, lostweight, lostweight_per,
                 totaltesttime, constpower, phenocode, flametime, flameduration,
                 maxtf1, maxtf2, maxts, maxtc,
                 maxtf1_time, maxtf2_time, maxts_time, maxtc_time,
                 finaltf1, finaltf2, finalts, finaltc,
                 finaltf1_time, finaltf2_time, finalts_time, finaltc_time,
                 deltatf1, deltatf2, deltatf, deltats, deltatc, memo, flag)
            VALUES
                ($pid, $tid, $tdate, $ambtemp, $ambhumi, $according, $op,
                 $appid, $appname, $appchkdate, $rptno,
                 $prewt, $postwt, $lostwt, $lostwtper,
                 $ttime, $cpow, $pheno, $ftime, $fdur,
                 $maxtf1, $maxtf2, $maxts, $maxtc,
                 $maxtf1t, $maxtf2t, $maxtst, $maxtct,
                 $ftf1, $ftf2, $fts, $ftc,
                 $ftf1t, $ftf2t, $ftst, $ftct,
                 $dtf1, $dtf2, $dtf, $dts, $dtc, $memo, $flag)";
        cmd.Parameters.AddWithValue("$pid", record.ProductId);
        cmd.Parameters.AddWithValue("$tid", record.TestId);
        cmd.Parameters.AddWithValue("$tdate", record.TestDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$ambtemp", record.AmbTemp);
        cmd.Parameters.AddWithValue("$ambhumi", record.AmbHumi);
        cmd.Parameters.AddWithValue("$according", record.According);
        cmd.Parameters.AddWithValue("$op", record.Operator);
        cmd.Parameters.AddWithValue("$appid", record.ApparatusId);
        cmd.Parameters.AddWithValue("$appname", record.ApparatusName);
        cmd.Parameters.AddWithValue("$appchkdate", record.ApparatusChkDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$rptno", record.RptNo);
        cmd.Parameters.AddWithValue("$prewt", record.PreWeight);
        cmd.Parameters.AddWithValue("$postwt", record.PostWeight);
        cmd.Parameters.AddWithValue("$lostwt", record.LostWeight);
        cmd.Parameters.AddWithValue("$lostwtper", record.LostWeightPer);
        cmd.Parameters.AddWithValue("$ttime", record.TotalTestTime);
        cmd.Parameters.AddWithValue("$cpow", record.ConstPower);
        cmd.Parameters.AddWithValue("$pheno", record.PhenoCode);
        cmd.Parameters.AddWithValue("$ftime", record.FlameTime);
        cmd.Parameters.AddWithValue("$fdur", record.FlameDuration);
        cmd.Parameters.AddWithValue("$maxtf1", record.MaxTf1);
        cmd.Parameters.AddWithValue("$maxtf2", record.MaxTf2);
        cmd.Parameters.AddWithValue("$maxts", record.MaxTs);
        cmd.Parameters.AddWithValue("$maxtc", record.MaxTc);
        cmd.Parameters.AddWithValue("$maxtf1t", record.MaxTf1Time);
        cmd.Parameters.AddWithValue("$maxtf2t", record.MaxTf2Time);
        cmd.Parameters.AddWithValue("$maxtst", record.MaxTsTime);
        cmd.Parameters.AddWithValue("$maxtct", record.MaxTcTime);
        cmd.Parameters.AddWithValue("$ftf1", record.FinalTf1);
        cmd.Parameters.AddWithValue("$ftf2", record.FinalTf2);
        cmd.Parameters.AddWithValue("$fts", record.FinalTs);
        cmd.Parameters.AddWithValue("$ftc", record.FinalTc);
        cmd.Parameters.AddWithValue("$ftf1t", record.FinalTf1Time);
        cmd.Parameters.AddWithValue("$ftf2t", record.FinalTf2Time);
        cmd.Parameters.AddWithValue("$ftst", record.FinalTsTime);
        cmd.Parameters.AddWithValue("$ftct", record.FinalTcTime);
        cmd.Parameters.AddWithValue("$dtf1", record.DeltaTf1);
        cmd.Parameters.AddWithValue("$dtf2", record.DeltaTf2);
        cmd.Parameters.AddWithValue("$dtf", record.DeltaTf);
        cmd.Parameters.AddWithValue("$dts", record.DeltaTs);
        cmd.Parameters.AddWithValue("$dtc", record.DeltaTc);
        cmd.Parameters.AddWithValue("$memo", record.Memo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$flag", record.Flag ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateTestResult(string productId, string testId, double preWeight,
                                  double postWeight, double lostPer, double deltaTf,
                                  double deltaTf1, double deltaTf2, double deltaTs, double deltaTc,
                                  int totalTime, string phenoCode, int flameTime, int flameDuration,
                                  double maxTf1, double maxTf2, double maxTs, double maxTc,
                                  int maxTf1Time, int maxTf2Time, int maxTsTime, int maxTcTime,
                                  double finalTf1, double finalTf2, double finalTs, double finalTc,
                                  int finalTf1Time, int finalTf2Time, int finalTsTime, int finalTcTime,
                                  string? memo)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE testmaster SET
                postweight      = $post,
                lostweight      = $lost,
                lostweight_per  = $lostper,
                deltatf         = $dtf,
                deltatf1        = $dtf1,
                deltatf2        = $dtf2,
                deltats         = $dts,
                deltatc         = $dtc,
                totaltesttime   = $time,
                phenocode       = $pheno,
                flametime       = $ftime,
                flameduration   = $fdur,
                maxtf1          = $maxtf1,
                maxtf2          = $maxtf2,
                maxts           = $maxts,
                maxtc           = $maxtc,
                maxtf1_time     = $maxtf1t,
                maxtf2_time     = $maxtf2t,
                maxts_time      = $maxtst,
                maxtc_time      = $maxtct,
                finaltf1        = $ftf1,
                finaltf2        = $ftf2,
                finalts         = $fts,
                finaltc         = $ftc,
                finaltf1_time   = $ftf1t,
                finaltf2_time   = $ftf2t,
                finalts_time    = $ftst,
                finaltc_time    = $ftct,
                memo            = $memo,
                flag            = '10000000'
            WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$post", postWeight);
        cmd.Parameters.AddWithValue("$lost", preWeight - postWeight);
        cmd.Parameters.AddWithValue("$lostper", lostPer);
        cmd.Parameters.AddWithValue("$dtf", deltaTf);
        cmd.Parameters.AddWithValue("$dtf1", deltaTf1);
        cmd.Parameters.AddWithValue("$dtf2", deltaTf2);
        cmd.Parameters.AddWithValue("$dts", deltaTs);
        cmd.Parameters.AddWithValue("$dtc", deltaTc);
        cmd.Parameters.AddWithValue("$time", totalTime);
        cmd.Parameters.AddWithValue("$pheno", phenoCode);
        cmd.Parameters.AddWithValue("$ftime", flameTime);
        cmd.Parameters.AddWithValue("$fdur", flameDuration);
        cmd.Parameters.AddWithValue("$maxtf1", maxTf1);
        cmd.Parameters.AddWithValue("$maxtf2", maxTf2);
        cmd.Parameters.AddWithValue("$maxts", maxTs);
        cmd.Parameters.AddWithValue("$maxtc", maxTc);
        cmd.Parameters.AddWithValue("$maxtf1t", maxTf1Time);
        cmd.Parameters.AddWithValue("$maxtf2t", maxTf2Time);
        cmd.Parameters.AddWithValue("$maxtst", maxTsTime);
        cmd.Parameters.AddWithValue("$maxtct", maxTcTime);
        cmd.Parameters.AddWithValue("$ftf1", finalTf1);
        cmd.Parameters.AddWithValue("$ftf2", finalTf2);
        cmd.Parameters.AddWithValue("$fts", finalTs);
        cmd.Parameters.AddWithValue("$ftc", finalTc);
        cmd.Parameters.AddWithValue("$ftf1t", finalTf1Time);
        cmd.Parameters.AddWithValue("$ftf2t", finalTf2Time);
        cmd.Parameters.AddWithValue("$ftst", finalTsTime);
        cmd.Parameters.AddWithValue("$ftct", finalTcTime);
        cmd.Parameters.AddWithValue("$memo", memo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        cmd.ExecuteNonQuery();
    }

    public List<TestMasterRecord> QueryTests(DateTime from, DateTime to, string? productId = null, string? operatorName = null)
    {
        var list = new List<TestMasterRecord>();
        using var conn = CreateConnection();
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

        cmd.CommandText = $"SELECT * FROM testmaster WHERE {string.Join(" AND ", conditions)} ORDER BY testdate DESC, testid DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadTestMasterRecord(reader));
        }
        return list;
    }

    public TestMasterRecord? GetTest(string productId, string testId)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMasterRecord(reader);
        return null;
    }

    public TestMasterRecord? CheckUnfinishedTest()
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        // 查找 totaltesttime > 0 且 flag 不是 '10000000' 的记录
        cmd.CommandText = "SELECT * FROM testmaster WHERE totaltesttime > 0 AND (flag IS NULL OR flag != '10000000') ORDER BY testdate DESC LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return ReadTestMasterRecord(reader);
        return null;
    }

    public List<string> GetDistinctOperators()
    {
        var list = new List<string>();
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT operator FROM testmaster ORDER BY operator";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(reader.GetString(0));
        return list;
    }

    private TestMasterRecord ReadTestMasterRecord(SqliteDataReader reader)
    {
        return new TestMasterRecord
        {
            ProductId = reader.GetString(0),
            TestId = reader.GetString(1),
            TestDate = DateTime.Parse(reader.GetString(2)),
            AmbTemp = reader.GetDouble(3),
            AmbHumi = reader.GetDouble(4),
            According = reader.GetString(5),
            Operator = reader.GetString(6),
            ApparatusId = reader.GetString(7),
            ApparatusName = reader.GetString(8),
            ApparatusChkDate = DateTime.Parse(reader.GetString(9)),
            RptNo = reader.GetString(10),
            PreWeight = reader.GetDouble(11),
            PostWeight = reader.GetDouble(12),
            LostWeight = reader.GetDouble(13),
            LostWeightPer = reader.GetDouble(14),
            TotalTestTime = reader.GetInt32(15),
            ConstPower = reader.GetInt32(16),
            PhenoCode = reader.GetString(17),
            FlameTime = reader.GetInt32(18),
            FlameDuration = reader.GetInt32(19),
            MaxTf1 = reader.GetDouble(20),
            MaxTf2 = reader.GetDouble(21),
            MaxTs = reader.GetDouble(22),
            MaxTc = reader.GetDouble(23),
            MaxTf1Time = reader.GetInt32(24),
            MaxTf2Time = reader.GetInt32(25),
            MaxTsTime = reader.GetInt32(26),
            MaxTcTime = reader.GetInt32(27),
            FinalTf1 = reader.GetDouble(28),
            FinalTf2 = reader.GetDouble(29),
            FinalTs = reader.GetDouble(30),
            FinalTc = reader.GetDouble(31),
            FinalTf1Time = reader.GetInt32(32),
            FinalTf2Time = reader.GetInt32(33),
            FinalTsTime = reader.GetInt32(34),
            FinalTcTime = reader.GetInt32(35),
            DeltaTf1 = reader.GetDouble(36),
            DeltaTf2 = reader.GetDouble(37),
            DeltaTf = reader.GetDouble(38),
            DeltaTs = reader.GetDouble(39),
            DeltaTc = reader.GetDouble(40),
            Memo = reader.IsDBNull(41) ? null : reader.GetString(41),
            Flag = reader.IsDBNull(42) ? null : reader.GetString(42)
        };
    }

    // ==================== 校准记录 ====================

    public void InsertCalibrationRecord(CalibrationRecord record)
    {
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO CalibrationRecords
                (Id, CalibrationDate, CalibrationType, ApparatusId, Operator, TemperatureData,
                 UniformityResult, MaxDeviation, AverageTemperature, PassedCriteria, Remarks, CreatedAt,
                 TempA1, TempA2, TempA3, TempB1, TempB2, TempB3, TempC1, TempC2, TempC3,
                 TAvg, TAvgAxis1, TAvgAxis2, TAvgAxis3, TAvgLevela, TAvgLevelb, TAvgLevelc,
                 TDevAxis1, TDevAxis2, TDevAxis3, TDevLevela, TDevLevelb, TDevLevelc,
                 TAvgDevAxis, TAvgDevLevel, CenterTempData, Memo)
            VALUES
                ($id, $caldate, $caltype, $appid, $op, $tempdata,
                 $unires, $maxdev, $avgtemp, $passed, $remarks, $created,
                 $ta1, $ta2, $ta3, $tb1, $tb2, $tb3, $tc1, $tc2, $tc3,
                 $tavg, $tavga1, $tavga2, $tavga3, $tavglva, $tavglvb, $tavglvc,
                 $tdeva1, $tdeva2, $tdeva3, $tdevlva, $tdevlvb, $tdevlvc,
                 $tavgdeva, $tavgdevl, $ctempdata, $memo)";
        cmd.Parameters.AddWithValue("$id", record.Id);
        cmd.Parameters.AddWithValue("$caldate", record.CalibrationDate);
        cmd.Parameters.AddWithValue("$caltype", record.CalibrationType);
        cmd.Parameters.AddWithValue("$appid", record.ApparatusId);
        cmd.Parameters.AddWithValue("$op", record.Operator);
        cmd.Parameters.AddWithValue("$tempdata", record.TemperatureData);
        cmd.Parameters.AddWithValue("$unires", record.UniformityResult ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$maxdev", record.MaxDeviation ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$avgtemp", record.AverageTemperature ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$passed", record.PassedCriteria);
        cmd.Parameters.AddWithValue("$remarks", record.Remarks);
        cmd.Parameters.AddWithValue("$created", record.CreatedAt);
        cmd.Parameters.AddWithValue("$ta1", record.TempA1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ta2", record.TempA2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ta3", record.TempA3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb1", record.TempB1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb2", record.TempB2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tb3", record.TempB3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc1", record.TempC1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc2", record.TempC2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tc3", record.TempC3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavg", record.TAvg ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavga1", record.TAvgAxis1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavga2", record.TAvgAxis2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavga3", record.TAvgAxis3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglva", record.TAvgLevela ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglvb", record.TAvgLevelb ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavglvc", record.TAvgLevelc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdeva1", record.TDevAxis1 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdeva2", record.TDevAxis2 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdeva3", record.TDevAxis3 ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlva", record.TDevLevela ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlvb", record.TDevLevelb ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tdevlvc", record.TDevLevelc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgdeva", record.TAvgDevAxis ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$tavgdevl", record.TAvgDevLevel ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ctempdata", record.CenterTempData ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$memo", record.Memo ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<CalibrationRecord> GetCalibrationRecords()
    {
        var list = new List<CalibrationRecord>();
        using var conn = CreateConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CalibrationRecords ORDER BY CalibrationDate DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CalibrationRecord
            {
                Id = reader.GetString(0),
                CalibrationDate = reader.GetString(1),
                CalibrationType = reader.GetString(2),
                ApparatusId = reader.GetInt32(3),
                Operator = reader.GetString(4),
                TemperatureData = reader.GetString(5),
                UniformityResult = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                MaxDeviation = reader.IsDBNull(7) ? null : reader.GetDouble(7),
                AverageTemperature = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                PassedCriteria = reader.GetInt32(9),
                Remarks = reader.GetString(10),
                CreatedAt = reader.GetString(11),
                TempA1 = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                TempA2 = reader.IsDBNull(13) ? null : reader.GetDouble(13),
                TempA3 = reader.IsDBNull(14) ? null : reader.GetDouble(14),
                TempB1 = reader.IsDBNull(15) ? null : reader.GetDouble(15),
                TempB2 = reader.IsDBNull(16) ? null : reader.GetDouble(16),
                TempB3 = reader.IsDBNull(17) ? null : reader.GetDouble(17),
                TempC1 = reader.IsDBNull(18) ? null : reader.GetDouble(18),
                TempC2 = reader.IsDBNull(19) ? null : reader.GetDouble(19),
                TempC3 = reader.IsDBNull(20) ? null : reader.GetDouble(20),
                TAvg = reader.IsDBNull(21) ? null : reader.GetDouble(21),
                TAvgAxis1 = reader.IsDBNull(22) ? null : reader.GetDouble(22),
                TAvgAxis2 = reader.IsDBNull(23) ? null : reader.GetDouble(23),
                TAvgAxis3 = reader.IsDBNull(24) ? null : reader.GetDouble(24),
                TAvgLevela = reader.IsDBNull(25) ? null : reader.GetDouble(25),
                TAvgLevelb = reader.IsDBNull(26) ? null : reader.GetDouble(26),
                TAvgLevelc = reader.IsDBNull(27) ? null : reader.GetDouble(27),
                TDevAxis1 = reader.IsDBNull(28) ? null : reader.GetDouble(28),
                TDevAxis2 = reader.IsDBNull(29) ? null : reader.GetDouble(29),
                TDevAxis3 = reader.IsDBNull(30) ? null : reader.GetDouble(30),
                TDevLevela = reader.IsDBNull(31) ? null : reader.GetDouble(31),
                TDevLevelb = reader.IsDBNull(32) ? null : reader.GetDouble(32),
                TDevLevelc = reader.IsDBNull(33) ? null : reader.GetDouble(33),
                TAvgDevAxis = reader.IsDBNull(34) ? null : reader.GetDouble(34),
                TAvgDevLevel = reader.IsDBNull(35) ? null : reader.GetDouble(35),
                CenterTempData = reader.IsDBNull(36) ? null : reader.GetString(36),
                Memo = reader.IsDBNull(37) ? null : reader.GetString(37)
            });
        }
        return list;
    }
}