using ISO11820.Data;
using ISO11820.Models;
using ISO11820.Services;

namespace ISO11820.Core;

public class TestController
{
    private readonly DbHelper _db;
    private readonly DaqWorker _daqWorker;
    private readonly SimulationConfig _simConfig;

    public TestState State { get; private set; } = TestState.Idle;
    public TestMaster? CurrentTest { get; private set; }
    public int RecordSeconds => _daqWorker.ElapsedSeconds;
    public List<TemperatureData> TemperatureHistory { get; } = new();

    private int _stableTickCount;
    private readonly List<double> _pidOutputQueue = new();
    private readonly List<double> _driftHistory = new(); // 只在稳定范围内累积，用于温漂计算

    public event EventHandler<string>? StateChanged;

    public TestController(DbHelper db, DaqWorker daqWorker, SimulationConfig simConfig)
    {
        _db = db;
        _daqWorker = daqWorker;
        _simConfig = simConfig;

        _daqWorker.SecondElapsed += OnSecondElapsed;
    }

    public void DoWork()
    {
        if (State == TestState.Idle || CurrentTest == null) return;

        double tf1 = _daqWorker.Temperatures["TF1"];
        double maxDrift = _simConfig.MaxTemperatureDriftPerTenMinutes;
        double target = _simConfig.TargetFurnaceTemp;
        double threshold = _simConfig.StableThreshold;

        if (State == TestState.Preparing)
        {
            bool inRange = tf1 >= (target - threshold) && tf1 <= (target + threshold);

            if (inRange)
            {
                _stableTickCount++;
                _driftHistory.Add(tf1);
                if (_driftHistory.Count > 600) _driftHistory.RemoveAt(0);

                // 需要足够的稳定数据和足够的 tick 数
                if (_stableTickCount > 3 && _driftHistory.Count >= 10)
                {
                    double drift = DriftCalculator.CalculateDrift(_driftHistory);
                    if (!double.IsNaN(drift) && Math.Abs(drift) <= maxDrift)
                    {
                        TransitionTo(TestState.Ready);
                        _daqWorker.AddMessage("温度已稳定，可以开始记录");
                    }
                }
            }
            else
            {
                _stableTickCount = 0;
                _driftHistory.Clear();
            }

            _pidOutputQueue.Add(tf1);
            if (_pidOutputQueue.Count > 600) _pidOutputQueue.RemoveAt(0);
        }

        if (State == TestState.Ready)
        {
            _pidOutputQueue.Add(tf1);
            if (_pidOutputQueue.Count > 600) _pidOutputQueue.RemoveAt(0);

            bool inRange = tf1 >= (target - threshold) && tf1 <= (target + threshold);
            if (inRange)
            {
                _driftHistory.Add(tf1);
                if (_driftHistory.Count > 600) _driftHistory.RemoveAt(0);
            }
            else
            {
                _stableTickCount = 0;
                _driftHistory.Clear();
                TransitionTo(TestState.Preparing);
            }
        }

        if (State == TestState.Recording)
            CheckTerminationCondition();
    }

    private void OnSecondElapsed()
    {
        if (State != TestState.Recording || CurrentTest == null) return;

        var temps = _daqWorker.Temperatures;
        TemperatureHistory.Add(new TemperatureData
        {
            Time = _daqWorker.ElapsedSeconds,
            Temp1 = temps["TF1"],
            Temp2 = temps["TF2"],
            TempSurface = temps["TS"],
            TempCenter = temps["TC"],
            TempCalibration = temps["TCal"]
        });
    }

    private void CheckTerminationCondition()
    {
        if (CurrentTest == null) return;
        int targetDuration = CurrentTest.DurationMode == "Standard" ? 3600 : CurrentTest.TargetDuration;
        int secs = _daqWorker.ElapsedSeconds;

        if (secs >= targetDuration)
        {
            _daqWorker.AddMessage($"记录时间到达 {targetDuration} 秒，试验自动结束");
            TransitionTo(TestState.Complete);
            return;
        }

        if (CurrentTest.DurationMode == "Standard" && secs >= 1800 && secs % 300 == 0)
        {
            double drift = DriftCalculator.CalculateDrift(_driftHistory);
            double maxDrift = _simConfig.MaxTemperatureDriftPerTenMinutes;
            if (!double.IsNaN(drift) && Math.Abs(drift) <= maxDrift)
            {
                _daqWorker.AddMessage("满足终止条件，试验结束");
                TransitionTo(TestState.Complete);
            }
        }
    }

    public bool StartHeating()
    {
        if (State != TestState.Idle) return false;
        TransitionTo(TestState.Preparing);
        _daqWorker.AddMessage("开始升温，系统升温中");
        return true;
    }

    public bool StopHeating()
    {
        if (State != TestState.Preparing && State != TestState.Ready && State != TestState.Complete) return false;
        TransitionTo(TestState.Idle);
        _daqWorker.AddMessage("停止加热，系统冷却中");
        return true;
    }

    public bool StartRecording()
    {
        if (State != TestState.Ready) return false;

        if (_pidOutputQueue.Count > 0 && CurrentTest != null)
            CurrentTest.ConstPower = _pidOutputQueue.Average();

        _daqWorker.ResetElapsed();
        TemperatureHistory.Clear();
        TransitionTo(TestState.Recording);
        _daqWorker.AddMessage("开始记录，计时开始");
        return true;
    }

    public bool StopRecording()
    {
        if (State != TestState.Recording) return false;

        if (_daqWorker.ElapsedSeconds > 0)
        {
            _daqWorker.AddMessage("用户手动停止记录");
            TransitionTo(TestState.Complete);
        }
        else
        {
            TransitionTo(TestState.Preparing);
        }
        return true;
    }

    public void CreateTest(TestMaster test, ProductMaster product)
    {
        _db.InsertProduct(product);
        CurrentTest = test;
        _daqWorker.ResetElapsed();
        TemperatureHistory.Clear();
        _stableTickCount = 0;
        _pidOutputQueue.Clear();
        _driftHistory.Clear();
    }

    public void SaveTestRecord(double postWeight, int hasFlame, int flameStartTime,
        int flameDuration, string remark)
    {
        if (CurrentTest == null) return;

        var temps = _daqWorker.Temperatures;
        double envTemp = CurrentTest.EnvTemp;

        CurrentTest.PostWeight = postWeight;
        CurrentTest.LostWeight = CurrentTest.PreWeight - postWeight;
        CurrentTest.LostWeightPer = CurrentTest.PreWeight > 0
            ? CurrentTest.LostWeight / CurrentTest.PreWeight * 100 : 0;
        CurrentTest.HasFlame = hasFlame;
        CurrentTest.FlameStartTime = flameStartTime;
        CurrentTest.FlameDuration = flameDuration;
        CurrentTest.Remark = remark;
        CurrentTest.TotalTestTime = _daqWorker.ElapsedSeconds;
        CurrentTest.Flag = "10000000";

        CurrentTest.DeltaTf1 = temps["TF1"] - envTemp;
        CurrentTest.DeltaTf2 = temps["TF2"] - envTemp;
        CurrentTest.DeltaTs = temps["TS"] - envTemp;
        CurrentTest.DeltaTc = temps["TC"] - envTemp;
        CurrentTest.DeltaTf = CurrentTest.DeltaTs;

        _db.InsertTestMaster(CurrentTest);
    }

    public void ClearCurrentTest()
    {
        CurrentTest = null;
        _daqWorker.ResetElapsed();
        TemperatureHistory.Clear();
        _stableTickCount = 0;
        _pidOutputQueue.Clear();
        _driftHistory.Clear();

        if (State == TestState.Complete)
            TransitionTo(TestState.Preparing);
    }

    public bool HasUnSavedCompleteTest()
    {
        return CurrentTest != null
            && CurrentTest.TotalTestTime > 0
            && CurrentTest.Flag != "10000000";
    }

    private void TransitionTo(TestState newState)
    {
        State = newState;
        _daqWorker.CurrentState = newState;
        StateChanged?.Invoke(this, newState.ToString());
    }
}
