using ISO11820.Core;
using ISO11820.Global;
using ISO11820.Models;
using ISO11820.UI.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using Serilog;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.UI.Forms;

public partial class MainForm : Form
{
    private readonly TestController _ctrl;
    private readonly AppContext _ctx;

    // 图表
    private PlotView _plotView = null!;
    private PlotModel _plotModel = null!;
    private LineSeries _sTF1 = null!, _sTF2 = null!, _sTS = null!, _sTC = null!;
    private int _tick;

    // 状态
    private Label _lblState = null!, _lblTime = null!, _lblSample = null!;
    private TemperaturePanel _tempPanel = null!;
    private RichTextBox _rtbLog = null!;

    // 按钮
    private Button _btnNew = null!, _btnHeat = null!, _btnStop = null!;
    private Button _btnRec = null!, _btnEnd = null!, _btnSave = null!;
    private Button _btnExport = null!;

    // 查询
    private DateTimePicker _dtFrom = null!, _dtTo = null!;
    private TextBox _txtQProduct = null!;
    private DataGridView _dgvHistory = null!;

    // 校准
    private Label _lblCalVal = null!;
    private DataGridView _dgvCalib = null!;

    public MainForm()
    {
        _ctx = AppContext.Instance;
        _ctrl = _ctx.TestController;
        BuildUI();
        SetupChart();
        _ctrl.DataBroadcast += OnTick;
        _ctrl.StateChanged += (_, _) => this.Invoke(UpdateButtons);
        UpdateButtons();
    }

    // ================================================================
    // 界面构建
    // ================================================================

    private void BuildUI()
    {
        this.Text = "ISO 11820 建筑材料不燃性试验仿真系统";
        this.Size = new Size(1200, 750);
        this.MinimumSize = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(245, 247, 250);
        this.ForeColor = Color.FromArgb(30, 30, 30);
        this.FormClosing += (s, e) =>
        {
            if (_ctrl.CurrentState == TestState.Recording)
            {
                if (MessageBox.Show("试验正在记录中，确定退出？", "确认", MessageBoxButtons.YesNo) == DialogResult.No)
                    e.Cancel = true;
            }
            if (!e.Cancel)
            {
                _ctrl.DataBroadcast -= OnTick;
            }
        };

        // ── 顶部导航栏 ──
        var nav = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(30, 90, 180), Padding = new Padding(12, 0, 12, 0) };

        var navTitle = new Label
        {
            Text = "ISO 11820  ·  " + (_ctx.CurrentUser?.UserName ?? ""),
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            Width = 300
        };
        nav.Controls.Add(navTitle);

        _lblState = NavBadge("空闲", Color.FromArgb(100, 100, 100));
        _lblTime = NavBadge("00:00", Color.FromArgb(80, 80, 80));
        _lblSample = NavBadge("--", Color.FromArgb(80, 80, 80));
        nav.Controls.AddRange(new Control[] { _lblState, _lblTime, _lblSample });

        this.Controls.Add(nav);

        // ── 内容区：TabControl ──
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Appearance = TabAppearance.FlatButtons,
            ItemSize = new Size(100, 34),
            SizeMode = TabSizeMode.Fixed
        };

        tabs.TabPages.Add(BuildMonitorTab());
        tabs.TabPages.Add(BuildHistoryTab());
        tabs.TabPages.Add(BuildCalibTab());
        this.Controls.Add(tabs);

        // ── 底部操作栏 ──
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Padding = new Padding(12, 6, 12, 6) };

        var sep1 = new Label { Text = "|", ForeColor = Color.LightGray, Size = new Size(12, 32), TextAlign = ContentAlignment.MiddleCenter };

        _btnNew = Btn("＋ 新建试验");
        _btnHeat = Btn("▶ 开始升温");
        _btnStop = Btn("■ 停止升温");
        var sep2 = new Label { Text = "|", ForeColor = Color.LightGray, Size = new Size(12, 32), TextAlign = ContentAlignment.MiddleCenter };
        _btnRec = Btn("● 开始记录");
        _btnEnd = Btn("■ 停止记录");
        var sep3 = new Label { Text = "|", ForeColor = Color.LightGray, Size = new Size(12, 32), TextAlign = ContentAlignment.MiddleCenter };
        _btnSave = Btn("📋 试验记录");
        _btnExport = Btn("📥 导出报告");

        _btnNew.Click += (_, _) => OpenNewTest();
        _btnHeat.Click += (_, _) => _ctrl.StartHeating();
        _btnStop.Click += (_, _) => _ctrl.StopHeating();
        _btnRec.Click += (_, _) => _ctrl.StartRecording();
        _btnEnd.Click += (_, _) => _ctrl.StopRecording();
        _btnSave.Click += (_, _) => OpenPhenomenon();
        _btnExport.Click += (_, _) => DoExport();

        bar.Controls.AddRange(new Control[] { _btnNew, sep1, _btnHeat, _btnStop, sep2, _btnRec, _btnEnd, sep3, _btnSave, _btnExport });
        this.Controls.Add(bar);
    }

    // ── Tab1: 实时监控 ──
    private TabPage BuildMonitorTab()
    {
        var tab = new TabPage(" 实时监控 ") { BackColor = Color.FromArgb(245, 247, 250) };

        // 左侧图表 70%
        _plotView = new PlotView { Dock = DockStyle.Fill, BackColor = Color.White };

        // 右侧面板
        var right = new Panel { Width = 255, Dock = DockStyle.Right, BackColor = Color.White, Padding = new Padding(8) };

        // 温度面板
        _tempPanel = new TemperaturePanel
        {
            Location = new Point(5, 5),
            Width = 218,
            Height = 300
        };
        right.Controls.Add(_tempPanel);

        // 分隔线
        var sep = new Label
        {
            Text = "─ 系统消息 ─",
            Location = new Point(8, 312),
            Size = new Size(214, 22),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray
        };
        right.Controls.Add(sep);

        // 消息日志
        _rtbLog = new RichTextBox
        {
            Location = new Point(5, 338),
            Size = new Size(218, 250),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(250, 250, 250),
            ForeColor = Color.FromArgb(60, 60, 60),
            Font = new Font("Consolas", 8),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };
        right.Controls.Add(_rtbLog);

        tab.Controls.Add(_plotView);
        tab.Controls.Add(right);
        return tab;
    }

    // ── Tab2: 记录查询 ──
    private TabPage BuildHistoryTab()
    {
        var tab = new TabPage(" 记录查询 ") { BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(12) };

        int y = 12;
        var lblFrom = new Label { Text = "日期范围:", Location = new Point(12, y + 4), Size = new Size(65, 22), Font = new Font("Segoe UI", 9) };
        _dtFrom = new DateTimePicker { Location = new Point(80, y), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddDays(-30) };
        var lblTo = new Label { Text = "至", Location = new Point(206, y + 4), Size = new Size(20, 22), Font = new Font("Segoe UI", 9) };
        _dtTo = new DateTimePicker { Location = new Point(226, y), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
        var lblPid = new Label { Text = "样品:", Location = new Point(360, y + 4), Size = new Size(40, 22), Font = new Font("Segoe UI", 9) };
        _txtQProduct = new TextBox { Location = new Point(400, y), Size = new Size(110, 25), Font = new Font("Segoe UI", 9) };
        var btnQ = new Button { Text = "查询", Location = new Point(520, y), Size = new Size(80, 28), BackColor = Color.FromArgb(30, 90, 180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnQ.Click += (_, _) => QueryHistory();
        tab.Controls.AddRange(new Control[] { lblFrom, _dtFrom, lblTo, _dtTo, lblPid, _txtQProduct, btnQ });

        _dgvHistory = new DataGridView
        {
            Location = new Point(12, 48),
            Size = new Size(950, 520),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };
        _dgvHistory.DoubleClick += (_, _) => OpenDetail();
        tab.Controls.Add(_dgvHistory);

        return tab;
    }

    // ── Tab3: 设备校准 ──
    private TabPage BuildCalibTab()
    {
        var tab = new TabPage(" 设备校准 ") { BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(12) };

        int y = 12;
        _lblCalVal = new Label
        {
            Text = "校准温度: -- °C",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 90, 180),
            Location = new Point(12, y),
            Size = new Size(300, 32)
        };
        tab.Controls.Add(_lblCalVal);

        var btnCalib = new Button
        {
            Text = "执行表面校准",
            Location = new Point(12, y + 40),
            Size = new Size(140, 34),
            BackColor = Color.FromArgb(30, 90, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnCalib.Click += (_, _) => DoCalibration();
        tab.Controls.Add(btnCalib);

        var btnRefresh = new Button
        {
            Text = "刷新历史",
            Location = new Point(162, y + 40),
            Size = new Size(100, 34),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9)
        };
        btnRefresh.Click += (_, _) => LoadCalibHistory();
        tab.Controls.Add(btnRefresh);

        _dgvCalib = new DataGridView
        {
            Location = new Point(12, 90),
            Size = new Size(950, 480),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.White,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };
        _dgvCalib.DoubleClick += (_, _) =>
        {
            if (_dgvCalib.SelectedRows.Count == 0) return;
            var id = _dgvCalib.SelectedRows[0].Cells[0].Value?.ToString() ?? "";
            var records = _ctx.CalibrationService.GetCalibrationHistory();
            var r = records.FirstOrDefault(x => x.Id == id);
            if (r != null) new CalibrationForm(r).ShowDialog(this);
        };
        tab.Controls.Add(_dgvCalib);

        return tab;
    }

    // ================================================================
    // 图表
    // ================================================================

    private void SetupChart()
    {
        _plotModel = new PlotModel
        {
            Title = "温度曲线",
            TitleColor = OxyColors.DarkGray,
            TextColor = OxyColors.Gray,
            PlotAreaBorderColor = OxyColor.FromRgb(220, 220, 220),
            PlotAreaBorderThickness = new OxyThickness(1)
        };
        _plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            Minimum = 0, Maximum = 600,
            TitleColor = OxyColors.Gray,
            AxislineColor = OxyColors.LightGray,
            TicklineColor = OxyColors.LightGray,
            TextColor = OxyColors.Gray,
            MajorGridlineStyle = LineStyle.Dash,
            MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(235, 235, 235),
            MajorStep = 60
        });
        _plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            Minimum = 0, Maximum = 800,
            TitleColor = OxyColors.Gray,
            AxislineColor = OxyColors.LightGray,
            TicklineColor = OxyColors.LightGray,
            TextColor = OxyColors.Gray,
            MajorGridlineStyle = LineStyle.Dash,
            MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(235, 235, 235),
            MajorStep = 100
        });

        _sTF1 = new LineSeries { Title = "炉温1", Color = OxyColor.FromRgb(220, 50, 50), StrokeThickness = 2 };
        _sTF2 = new LineSeries { Title = "炉温2", Color = OxyColor.FromRgb(50, 120, 220), StrokeThickness = 2 };
        _sTS = new LineSeries { Title = "表面温度", Color = OxyColor.FromRgb(40, 160, 80), StrokeThickness = 2 };
        _sTC = new LineSeries { Title = "中心温度", Color = OxyColor.FromRgb(240, 140, 30), StrokeThickness = 2, LineStyle = LineStyle.Dash };

        _plotModel.Series.Add(_sTF1);
        _plotModel.Series.Add(_sTF2);
        _plotModel.Series.Add(_sTS);
        _plotModel.Series.Add(_sTC);
        _plotView.Model = _plotModel;
    }

    // ================================================================
    // 数据更新
    // ================================================================

    private void OnTick(object? s, DataBroadcastEventArgs e)
    {
        if (this.InvokeRequired) { this.Invoke(() => OnTick(s, e)); return; }

        _tempPanel.UpdateValues(e.TF1, e.TF2, e.TS, e.TC, e.TCal);

        _lblState.Text = TextFor(e.CurrentState);
        _lblTime.Text = FormatTime(e.ElapsedSeconds);
        if (!string.IsNullOrEmpty(e.ProductId)) _lblSample.Text = e.ProductId;
        _lblCalVal.Text = $"校准温度: {e.TCal:F1} °C";

        _tick++;
        if (_tick % 5 == 0)
        {
            double t = _tick * 0.8;
            _sTF1.Points.Add(new DataPoint(t, e.TF1));
            _sTF2.Points.Add(new DataPoint(t, e.TF2));
            _sTS.Points.Add(new DataPoint(t, e.TS));
            _sTC.Points.Add(new DataPoint(t, e.TC));
            double xm = t, xn = Math.Max(0, xm - 600);
            _plotModel.Axes[0].Minimum = xn;
            _plotModel.Axes[0].Maximum = xm + 30;
            Trim(_sTF1); Trim(_sTF2); Trim(_sTS); Trim(_sTC);
            _plotView.InvalidatePlot(true);
        }

        foreach (var m in e.Messages)
            LogMsg(m.Time, m.Message);
    }

    private void Trim(LineSeries s) { while (s.Points.Count > 800) s.Points.RemoveAt(0); }

    private void LogMsg(string time, string msg)
    {
        _rtbLog.AppendText($"[{time}]  {msg}\n");
        if (_rtbLog.Lines.Length > 200)
            _rtbLog.Text = string.Join("\n", _rtbLog.Lines.Skip(Math.Max(0, _rtbLog.Lines.Length - 100)));
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }

    // ================================================================
    // 按钮状态
    // ================================================================

    private void UpdateButtons()
    {
        var st = _ctrl.CurrentState;
        var unsaved = _ctrl.HasUnsavedTest();

        SetBtn(_btnNew, st == TestState.Idle || (st == TestState.Preparing && !unsaved) || (st == TestState.Complete && !unsaved));
        SetBtn(_btnHeat, st == TestState.Idle);
        SetBtn(_btnStop, st == TestState.Preparing || st == TestState.Ready || st == TestState.Complete);
        SetBtn(_btnRec, st == TestState.Ready);
        SetBtn(_btnEnd, st == TestState.Recording);
        SetBtn(_btnSave, st == TestState.Complete);
        SetBtn(_btnExport, _ctrl.CurrentTest != null);
    }

    private void SetBtn(Button b, bool en)
    {
        b.Enabled = en;
        b.BackColor = en ? Color.FromArgb(30, 90, 180) : Color.LightGray;
        b.ForeColor = en ? Color.White : Color.Gray;
    }

    // ================================================================
    // 操作
    // ================================================================

    private void OpenNewTest()
    {
        using var f = new NewTestForm();
        if (f.ShowDialog(this) == DialogResult.OK) { UpdateButtons(); LogMsg(Now(), "新试验已创建"); }
    }

    private void OpenPhenomenon()
    {
        using var f = new PhenomenonRecordForm();
        if (f.ShowDialog(this) == DialogResult.OK) { UpdateButtons(); DoExport(); }
    }

    private void DoExport()
    {
        var t = _ctrl.CurrentTest;
        if (t == null) { MessageBox.Show("没有可导出的记录", "提示"); return; }
        try
        {
            var (csv, excel, pdf) = _ctx.ExportService.ExportAll(t.ProductId, t.TestId);
            LogMsg(Now(), $"报告已导出: {Path.GetFileName(excel)}");
            MessageBox.Show($"导出完成！\n\n{excel}\n{pdf}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"导出失败:\n{ex.Message}", "错误"); }
    }

    private void OpenDetail()
    {
        if (_dgvHistory.SelectedRows.Count == 0) return;
        var r = _dgvHistory.SelectedRows[0];
        new TestDetailForm(r.Cells[1].Value?.ToString() ?? "", r.Cells[0].Value?.ToString() ?? "").ShowDialog(this);
    }

    private void QueryHistory()
    {
        var list = _ctx.Db.QueryTests(_dtFrom.Value.Date, _dtTo.Value.Date.AddDays(1).AddSeconds(-1),
            string.IsNullOrWhiteSpace(_txtQProduct.Text) ? null : _txtQProduct.Text.Trim());
        _dgvHistory.DataSource = null;
        _dgvHistory.DataSource = list.Select(t => new
        {
            试验ID = t.TestId,
            样品编号 = t.ProductId,
            日期 = t.TestDate.ToString("yyyy-MM-dd"),
            操作员 = t.Operator,
            时长秒 = t.TotalTestTime,
            失重率 = $"{t.LostWeightPer:F1}%",
            温升 = $"{t.DeltaTf:F1}°C",
            状态 = t.Flag == "10000000" ? "✓" : "未保存"
        }).ToList();
        LogMsg(Now(), $"查询结果: {list.Count} 条");
    }

    private void DoCalibration()
    {
        try
        {
            var r = _ctx.CalibrationService.PerformSurfaceCalibration(0, _ctx.CurrentUser?.UserName ?? "");
            LogMsg(Now(), r.PassedCriteria == 1 ? $"校准通过 (偏差{r.MaxDeviation:F1}°C)" : $"校准未通过 (偏差{r.MaxDeviation:F1}°C)");
            LoadCalibHistory();
        }
        catch (Exception ex) { MessageBox.Show($"校准失败:\n{ex.Message}", "错误"); }
    }

    private void LoadCalibHistory()
    {
        var list = _ctx.CalibrationService.GetCalibrationHistory();
        _dgvCalib.DataSource = null;
        _dgvCalib.DataSource = list.Select(r => new
        {
            r.Id,
            日期 = r.CalibrationDate,
            类型 = r.CalibrationType,
            操作员 = r.Operator,
            均温 = $"{r.AverageTemperature:F1}°C",
            最大偏差 = $"{r.MaxDeviation:F2}°C",
            结果 = r.PassedCriteria == 1 ? "通过" : "未通过"
        }).ToList();
    }

    // ================================================================
    // 工具方法
    // ================================================================

    private static Button Btn(string text)
    {
        var b = new Button
        {
            Text = text,
            Size = new Size(105, 32),
            BackColor = Color.FromArgb(30, 90, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private static Label NavBadge(string text, Color bg)
    {
        return new Label
        {
            Text = $"  {text}  ",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = bg,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(6, 10, 6, 10),
            Dock = DockStyle.Right
        };
    }

    private static string TextFor(TestState s) => s switch
    {
        TestState.Idle => " 空闲 ",
        TestState.Preparing => " 升温中 ",
        TestState.Ready => " 就绪 ",
        TestState.Recording => " 记录中 ",
        TestState.Complete => " 完成 ",
        _ => s.ToString()
    };

    private static string FormatTime(int sec)
    {
        int m = sec / 60, ss = sec % 60;
        return $"{m:D2}:{ss:D2}";
    }

    private static string Now() => DateTime.Now.ToString("HH:mm:ss");
}
