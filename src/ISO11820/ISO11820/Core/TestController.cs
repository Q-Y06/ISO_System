using ISO11820.Data;
using ISO11820.Global;
using ISO11820.Models;
using ISO11820.Services;
using ISO11820.Services.Simulation;
using Serilog;

namespace ISO11820.Core;

/// <summary>
/// 试验控制器 — 状态机核心，协调仿真、数据采集和 UI 事件
/// </summary>
public class TestController
{
    private readonly DbHelper _db;
    private readonly SensorSimulator _simulator;
    private readonly SensorDataFileManager _fileManager;
    private readonly AppConfig _config;
    private readonly object _stateLock = new();

    // ===== 状态 =====
    public TestState CurrentState { get; private set; } = TestState.Idle;
    public TestMaster? CurrentTest { get; private set; }
    public ProductMaster? CurrentProduct { get; private set; }
    public int RecordElapsedSeconds { get; private set; }
    public int TargetDurationSeconds { get; private set; } = AppConstants.StandardTestDuration;
    public bool IsFixedDurationMode { get; private set; } = false;

    // 温度统计（记录阶段累加）
    public double MaxTf1 { get; private set; }
    public double MaxTf2 { get; private set; }
    public double MaxTs { get; private set; }
    public double MaxTc { get; private set; }
    public int MaxTf1Time { get; private set; }
    public int MaxTf2Time { get; private set; }
    public int MaxTsTime { get; private set; }
    public int MaxTcTime { get; private set; }

    // ===== 事件 =====
    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;
    public event EventHandler<TestState>? StateChanged;

    // ===== 构造 =====
    public TestController(DbHelper db, SensorSimulator simulator,
                          SensorDataFileManager fileManager, AppConfig config)
    {
        _db = db;
        _simulator = simulator;
        _fileManager = fileManager;
        _config = config;
    }

    // ================================================================
    // 公开命令（UI 按钮调用）
    // ================================================================

    /// <summary>
    /// 开始升温：Idle → Preparing
    /// </summary>
    public bool StartHeating()
    {
        if (!StateTransition.CanTransition(CurrentState, TestState.Preparing))
            return false;

        lock (_stateLock)
        {
            if (!StateTransition.CanTransition(CurrentState, TestState.Preparing))
                return false;
            TransitionTo(TestState.Preparing);
            AppendMessage("开始升温，系统升温中");
            return true;
        }
    }

    /// <summary>
    /// 停止升温：Preparing/Ready/Complete → Idle (或 Preparing)
    /// </summary>
    public bool StopHeating()
    {
        lock (_stateLock)
        {
            if (CurrentState == TestState.Preparing || CurrentState == TestState.Ready)
            {
                TransitionTo(TestState.Idle);
                _simulator.ResetStableCounter();
                AppendMessage("用户停止升温");
                return true;
            }
            else if (CurrentState == TestState.Complete)
            {
                TransitionTo(TestState.Preparing);
                AppendMessage("停止加热，保持恒温");
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 开始记录：Ready → Recording
    /// </summary>
    public bool StartRecording()
    {
        if (!StateTransition.CanTransition(CurrentState, TestState.Recording))
            return false;

        lock (_stateLock)
        {
            if (!StateTransition.CanTransition(CurrentState, TestState.Recording))
                return false;

            RecordElapsedSeconds = 0;
            ResetStatistics();
            TransitionTo(TestState.Recording);
            AppendMessage("开始记录，计时开始");

            // 创建 CSV 数据文件
            if (CurrentTest != null)
                _fileManager.CreateDataFile(CurrentTest.ProductId, CurrentTest.TestId);

            return true;
        }
    }

    /// <summary>
    /// 停止记录：Recording → Complete
    /// </summary>
    public bool StopRecording()
    {
        if (CurrentState != TestState.Recording)
            return false;

        lock (_stateLock)
        {
            if (CurrentState != TestState.Recording)
                return false;

            TransitionTo(TestState.Complete);
            AppendMessage("用户手动停止记录");
            return true;
        }
    }

    /// <summary>
    /// 创建新试验
    /// </summary>
    public bool CreateNewTest(ProductMaster product, TestMaster test, int targetDuration, bool isFixedDuration)
    {
        // 检查是否有未保存的试验
        if (_db.HasUnsavedCompletedTest())
            return false;

        lock (_stateLock)
        {
            if (_db.HasUnsavedCompletedTest())
                return false;

            // 保存样品
            if (!_db.ProductExists(product.ProductId))
                _db.InsertProduct(product);

            // 保存试验
            _db.InsertTest(test);

            CurrentProduct = product;
            CurrentTest = test;
            TargetDurationSeconds = targetDuration;
            IsFixedDurationMode = isFixedDuration;

            Log.Information("新建试验: ProductId={Pid}, TestId={Tid}", product.ProductId, test.TestId);
            AppendMessage($"新建试验：{product.ProductId} / {test.TestId}");
            return true;
        }
    }

    /// <summary>
    /// 保存试验结果（完成后的现象记录）
    /// </summary>
    public bool SaveTestResult(double postWeight, bool hasFlame, int flameTime,
                                int flameDuration, string? memo)
    {
        if (CurrentTest == null || CurrentProduct == null)
            return false;

        if (CurrentState != TestState.Complete)
            return false;

        var test = CurrentTest;
        test.PostWeight = postWeight;
        test.LostWeight = test.PreWeight - postWeight;
        test.LostWeightPer = test.PreWeight > 0
            ? Math.Round(test.LostWeight / test.PreWeight * 100, 2) : 0;

        // 火焰数据
        test.FlameTime = hasFlame ? flameTime : 0;
        test.FlameDuration = hasFlame ? flameDuration : 0;
        test.PhenoCode = hasFlame ? $"火焰:{flameTime}s/{flameDuration}s" : "无火焰";

        // 统计
        test.TotalTestTime = RecordElapsedSeconds;
        test.ConstPower = (int)_simulator.AveragePidOutput;
        test.MaxTf1 = MaxTf1;
        test.MaxTf2 = MaxTf2;
        test.MaxTs = MaxTs;
        test.MaxTc = MaxTc;
        test.MaxTf1Time = MaxTf1Time;
        test.MaxTf2Time = MaxTf2Time;
        test.MaxTsTime = MaxTsTime;
        test.MaxTcTime = MaxTcTime;

        test.FinalTf1 = _simulator.TF1;
        test.FinalTf2 = _simulator.TF2;
        test.FinalTs = _simulator.TS;
        test.FinalTc = _simulator.TC;
        test.FinalTf1Time = RecordElapsedSeconds;
        test.FinalTf2Time = RecordElapsedSeconds;
        test.FinalTsTime = RecordElapsedSeconds;
        test.FinalTcTime = RecordElapsedSeconds;

        // 温升 = 最终值 - 环境温度
        test.DeltaTf1 = Math.Round(test.FinalTf1 - CurrentTest.AmbTemp, 2);
        test.DeltaTf2 = Math.Round(test.FinalTf2 - CurrentTest.AmbTemp, 2);
        test.DeltaTs = Math.Round(test.FinalTs - CurrentTest.AmbTemp, 2);
        test.DeltaTc = Math.Round(test.FinalTc - CurrentTest.AmbTemp, 2);
        test.DeltaTf = test.DeltaTs; // 综合温升取表面温升

        test.Memo = memo;
        test.Flag = AppConstants.TestCompleteFlag;

        _db.UpdateTestResult(test);

        Log.Information("试验结果已保存: ProductId={Pid}, TestId={Tid}, LostPer={L}%, DeltaTf={D}",
            test.ProductId, test.TestId, test.LostWeightPer, test.DeltaTf);
        AppendMessage("试验记录已保存");

        // 切换到 Preparing（保持炉温），等待下次试验
        TransitionTo(TestState.Preparing);

        return true;
    }

    /// <summary>
    /// 是否有未保存的试验记录
    /// </summary>
    public bool HasUnsavedTest()
    {
        return _db.HasUnsavedCompletedTest();
    }

    // ================================================================
    // 内部方法（由 DaqWorker 调用）
    // ================================================================

    /// <summary>
    /// 每 800ms 由 DaqWorker 调用一次
    /// </summary>
    public void OnDaqTick()
    {
        lock (_stateLock)
        {
            bool isRecording = CurrentState == TestState.Recording;
            _simulator.Update(CurrentState, isRecording, RecordElapsedSeconds);

            // 检查自动转换条件
            CheckAutoTransition();

            // 记录阶段写入 CSV
            if (isRecording && CurrentTest != null)
            {
                WriteDataPoint();
                UpdateStatistics();
                RecordElapsedSeconds++;
            }

            // 触发数据广播事件
            FireDataBroadcast();
        }
    }

    // ================================================================
    // 自动转换检查
    // ================================================================

    private void CheckAutoTransition()
    {
        switch (CurrentState)
        {
            case TestState.Preparing:
                // 检查是否稳定
                if (_simulator.IsStable &&
                    _simulator.TF1 >= AppConstants.StableTempLower &&
                    _simulator.TF1 <= AppConstants.StableTempUpper)
                {
                    TransitionTo(TestState.Ready);
                    AppendMessage("温度已稳定，可以开始记录");
                }
                break;

            case TestState.Ready:
                // 温度跌出稳定范围 → 回退 Preparing
                if (_simulator.TF1 < AppConstants.StableTempLower ||
                    _simulator.TF1 > AppConstants.StableTempUpper)
                {
                    _simulator.ResetStableCounter();
                    TransitionTo(TestState.Preparing);
                    AppendMessage("温度波动，重新升温");
                }
                break;

            case TestState.Recording:
                // 到达时长 → 自动完成
                if (RecordElapsedSeconds >= TargetDurationSeconds)
                {
                    TransitionTo(TestState.Complete);
                    AppendMessage($"记录时间到达 {TargetDurationSeconds} 秒，试验自动结束");
                }
                // 非固定时长模式：检查提前终止条件（在第30、35、40、45、50、55分钟）
                else if (!IsFixedDurationMode &&
                         RecordElapsedSeconds >= 1800 &&
                         RecordElapsedSeconds % 300 < 1)
                {
                    CheckEarlyTermination();
                }
                break;
        }
    }

    /// <summary>
    /// 检查是否可以提前终止（标准模式，每5分钟检查一次）
    /// </summary>
    private void CheckEarlyTermination()
    {
        var tf1History = _simulator.GetTF1History();
        var tf2History = _simulator.GetTF2History();

        if (tf1History.Count < 30 || tf2History.Count < 30) return;

        double drift1 = DriftCalculator.CalculateDrift(tf1History);
        double drift2 = DriftCalculator.CalculateDrift(tf2History);

        if (DriftCalculator.IsWithinDriftLimit(drift1, drift2, AppConstants.MaxDriftPer10Min))
        {
            TransitionTo(TestState.Complete);
            AppendMessage("满足终止条件，试验结束");
        }
    }

    // ================================================================
    // 数据记录
    // ================================================================

    private void WriteDataPoint()
    {
        if (CurrentTest == null) return;

        var point = new SensorDataPoint
        {
            Time = RecordElapsedSeconds,
            Temp1 = _simulator.TF1,
            Temp2 = _simulator.TF2,
            TempSurface = _simulator.TS,
            TempCenter = _simulator.TC,
            TempCalibration = _simulator.TCal
        };
        _fileManager.AppendRow(CurrentTest.ProductId, CurrentTest.TestId, point);
    }

    private void UpdateStatistics()
    {
        if (_simulator.TF1 > MaxTf1) { MaxTf1 = _simulator.TF1; MaxTf1Time = RecordElapsedSeconds; }
        if (_simulator.TF2 > MaxTf2) { MaxTf2 = _simulator.TF2; MaxTf2Time = RecordElapsedSeconds; }
        if (_simulator.TS > MaxTs) { MaxTs = _simulator.TS; MaxTsTime = RecordElapsedSeconds; }
        if (_simulator.TC > MaxTc) { MaxTc = _simulator.TC; MaxTcTime = RecordElapsedSeconds; }
    }

    private void ResetStatistics()
    {
        MaxTf1 = MaxTf2 = MaxTs = MaxTc = 0;
        MaxTf1Time = MaxTf2Time = MaxTsTime = MaxTcTime = 0;
    }

    // ================================================================
    // 状态转换与事件
    // ================================================================

    private void TransitionTo(TestState newState)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        Log.Debug("状态转换: {Old} -> {New}", oldState, newState);

        // 不在锁内触发事件，避免死锁
        Task.Run(() => StateChanged?.Invoke(this, newState));
    }

    private readonly List<MasterMessage> _pendingMessages = new();

    private void AppendMessage(string msg)
    {
        _pendingMessages.Add(new MasterMessage
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Message = msg
        });
    }

    private void FireDataBroadcast()
    {
        double drift = 0;
        var tf1History = _simulator.GetTF1History();
        if (tf1History.Count >= 30)
            drift = DriftCalculator.CalculateDrift(tf1History);

        var args = new DataBroadcastEventArgs
        {
            CurrentState = CurrentState,
            TF1 = Math.Round(_simulator.TF1, 1),
            TF2 = Math.Round(_simulator.TF2, 1),
            TS = Math.Round(_simulator.TS, 1),
            TC = Math.Round(_simulator.TC, 1),
            TCal = Math.Round(_simulator.TCal, 1),
            ElapsedSeconds = RecordElapsedSeconds,
            TemperatureDrift = Math.Round(drift, 2),
            Messages = new List<MasterMessage>(_pendingMessages),
            Timestamp = DateTime.Now,
            ProductId = CurrentProduct?.ProductId,
            TestId = CurrentTest?.TestId
        };

        _pendingMessages.Clear();

        // 异步触发事件，避免阻塞 DAQ 线程
        Task.Run(() => DataBroadcast?.Invoke(this, args));
    }
}
