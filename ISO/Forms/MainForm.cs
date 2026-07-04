using ISO11820.Core;
using ISO11820.Helpers;
using ISO11820.Models;
using ISO11820.Services;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;
using Serilog;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.Forms;

public partial class MainForm : Form
{
    private readonly AppContext _ctx = AppContext.Instance;
    private readonly TestController _tc;

    // Temperature display labels
    private Label lblTF1Val = null!, lblTF2Val = null!, lblTSVal = null!, lblTCVal = null!, lblTCalVal = null!;
    private Label lblStatus = null!, lblTimer = null!, lblDrift = null!, lblSample = null!;

    // 防频闪：缓存上次显示的温度值，只在值变化时才更新 Label
    private double _lastTF1 = double.MinValue, _lastTF2 = double.MinValue, _lastTS = double.MinValue, _lastTC = double.MinValue, _lastTCal = double.MinValue;

    // Buttons
    private Button btnNewTest = null!, btnStartHeat = null!, btnStopHeat = null!;
    private Button btnStartRecord = null!, btnStopRecord = null!, btnTestRecord = null!, btnSettings = null!;

    // Plot
    private PlotView plotView = null!;
    private PlotModel plotModel = null!;
    private LineSeries seriesTF1 = null!, seriesTF2 = null!, seriesTS = null!, seriesTC = null!;
    private readonly ToolTip _chartTooltip = new();
    private int _tickCounter;

    // Curve visibility checkboxes
    private CheckBox cbTF1 = null!, cbTF2 = null!, cbTS = null!, cbTC = null!;

    // Log
    private RichTextBox rtbLog = null!;

    // Tabs
    private TabControl tabControl = null!;
    private TabPage tabMain = null!, tabQuery = null!, tabCalibration = null!;

    // Query tab
    private DataGridView dgvRecords = null!;
    private DateTimePicker dtpStart = null!, dtpEnd = null!;
    private TextBox txtQueryProduct = null!;
    private ComboBox cmbQueryOperator = null!;
    // 详情卡片
    private Panel detailCardPanel = null!;
    private Label lblDetailTitle = null!, lblDetailVerdict = null!, lblDetailBody = null!;
    // 表格底部统计
    private Label lblRecordCount = null!;

    // Calibration tab
    private Label lblCalTemp = null!;
    private TextBox txtCalRefTemp = null!;
    private DataGridView dgvCalRecords = null!;

    public MainForm()
    {
        // 防频闪：开启 Form 级双缓冲
        this.DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        _tc = _ctx.TestController;
        InitializeComponent();
        SetupPlot();
        WireEvents();
        ShowInitialTemperatures();
        UpdateButtonStates();

        this.Load += (s, e) =>
        {
            rtbLog.SelectionColor = ThemeColors.TextSecondary;
            rtbLog.AppendText($"{DateTime.Now:HH:mm:ss}  系统初始化，操作员：{_ctx.CurrentOperator}\n");
            rtbLog.ScrollToCaret();
        };
    }

    private void ShowInitialTemperatures()
    {
        var temps = _ctx.DaqWorker.Temperatures;
        lblTF1Val.Text = $"{temps["TF1"]:F1} °C";
        lblTF2Val.Text = $"{temps["TF2"]:F1} °C";
        lblTSVal.Text = $"{temps["TS"]:F1} °C";
        lblTCVal.Text = $"{temps["TC"]:F1} °C";
        lblTCalVal.Text = $"{temps["TCal"]:F1} °C";
        lblCalTemp.Text = $"当前校准温: {temps["TCal"]:F1} °C";
    }

    #region UI Setup (自适应布局 + 大按钮)

    private void InitializeComponent()
    {
        this.Text = "ISO 11820 建筑材料不燃性试验系统";
        this.Size = new Size(1280, 820);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(900, 600);
        this.BackColor = ThemeColors.BgDark;
        this.ForeColor = ThemeColors.TextPrimary;

        tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei", 10) };

        // --- Tab 1: Main ---
        tabMain = new TabPage("试验控制");
        tabMain.BackColor = ThemeColors.BgDark;

        // === 顶部三列自适应布局 ===
        var topTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 160,
            BackColor = ThemeColors.BgDark,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(6, 6, 6, 6)
        };
        topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19F));
        topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
        topTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var tempPanel = CreateTemperaturePanel();
        var statusPanel = CreateStatusPanel();
        var buttonPanel = CreateButtonPanel();

        topTable.Controls.Add(tempPanel, 0, 0);
        topTable.Controls.Add(statusPanel, 1, 0);
        topTable.Controls.Add(buttonPanel, 2, 0);

        // === 图表 + 左侧曲线开关 ===
        plotView = new PlotView { Dock = DockStyle.Fill, BackColor = Color.White };
        var legendPanel = CreateCurveLegendPanel();

        var chartPanel = new Panel { Dock = DockStyle.Fill };
        chartPanel.Controls.Add(plotView);
        chartPanel.Controls.Add(legendPanel);

        // === 日志 ===
        rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColors.BgLog,
            ForeColor = ThemeColors.TextSecondary,
            Font = new Font("Consolas", 10),
            ReadOnly = true,
            WordWrap = true,
            BorderStyle = BorderStyle.None
        };

        var logTitlePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = ThemeColors.BgMedium
        };
        logTitlePanel.Controls.Add(new Label
        {
            Text = "▌ 系统消息",
            ForeColor = ThemeColors.TextMuted,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Location = new Point(8, 4),
            Size = new Size(200, 20)
        });

        var splitCenter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 320
        };
        splitCenter.Panel1.Controls.Add(chartPanel);
        splitCenter.Panel2.Controls.Add(rtbLog);
        splitCenter.Panel2.Controls.Add(logTitlePanel);

        var centerPanel = new Panel { Dock = DockStyle.Fill };
        centerPanel.Controls.Add(splitCenter);

        tabMain.Controls.Add(centerPanel);
        tabMain.Controls.Add(topTable);

        // --- Tab 2: Query ---
        tabQuery = new TabPage("记录查询");
        tabQuery.BackColor = ThemeColors.BgDark;
        BuildQueryTab();

        // --- Tab 3: Calibration ---
        tabCalibration = new TabPage("设备校准");
        tabCalibration.BackColor = ThemeColors.BgDark;
        BuildCalibrationTab();

        tabControl.TabPages.Add(tabMain);
        tabControl.TabPages.Add(tabQuery);
        tabControl.TabPages.Add(tabCalibration);

        this.Controls.Add(tabControl);
    }

    /// <summary>为控件递归开启双缓冲，消除频闪</summary>
    private static void EnableDoubleBuffering(Control control)
    {
        // Panel 等控件的 DoubleBuffered 是 protected 属性，通过反射设置
        var prop = typeof(Control).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        void SetDbl(Control c)
        {
            prop?.SetValue(c, true, null);
            foreach (Control child in c.Controls)
                SetDbl(child);
        }
        SetDbl(control);
    }

    /// <summary>温度面板 — 5 通道自适应排列</summary>
    private Panel CreateTemperaturePanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.BgDark };

        var ledTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1
        };
        for (int i = 0; i < 5; i++)
            ledTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        ledTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var labels = new[] { "炉温1", "炉温2", "表面温", "中心温", "校准温" };
        var colors = new[] { ThemeColors.TempFurnace1, ThemeColors.TempFurnace2, ThemeColors.TempSurface, ThemeColors.TempCenter, ThemeColors.TempCal };
        var lbls = new Label[5];

        for (int i = 0; i < 5; i++)
        {
            var ledPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.BgLed,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3)
            };

            var lblName = new Label
            {
                Text = labels[i],
                ForeColor = ThemeColors.TextMuted,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lbls[i] = new Label
            {
                Text = "0.0 °C",
                ForeColor = colors[i],
                BackColor = ThemeColors.BgLedValue,
                Font = new Font("Consolas", 18, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblChannel = new Label
            {
                Text = $"CH{i + 1}",
                ForeColor = ThemeColors.TextGray,
                Font = new Font("Consolas", 8),
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter
            };

            ledPanel.Controls.Add(lbls[i]);
            ledPanel.Controls.Add(lblName);
            ledPanel.Controls.Add(lblChannel);
            ledTable.Controls.Add(ledPanel, i, 0);
        }

        panel.Controls.Add(ledTable);
        lblTF1Val = lbls[0]; lblTF2Val = lbls[1]; lblTSVal = lbls[2]; lblTCVal = lbls[3]; lblTCalVal = lbls[4];

        // 温度面板所有子控件开启双缓冲，消除数字频闪
        EnableDoubleBuffering(panel);

        return panel;
    }

    /// <summary>状态面板 — 自适应行高</summary>
    private Panel CreateStatusPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColors.BgMedium,
            BorderStyle = BorderStyle.FixedSingle
        };

        var innerTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        innerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        innerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        innerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        innerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

        lblStatus = new Label { Text = "空闲", ForeColor = ThemeColors.TextPrimary, Font = new Font("Microsoft YaHei", 12, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        lblTimer = new Label { Text = "计时: 0 秒", ForeColor = ThemeColors.StatusCyan, Font = new Font("Consolas", 16, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        lblDrift = new Label { Text = "温漂: -- °C/10min", ForeColor = ThemeColors.TextSecondary, Font = new Font("Microsoft YaHei", 10), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        lblSample = new Label { Text = "样品: --", ForeColor = ThemeColors.TextSecondary, Font = new Font("Microsoft YaHei", 9), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

        innerTable.Controls.Add(lblStatus, 0, 0);
        innerTable.Controls.Add(lblTimer, 0, 1);
        innerTable.Controls.Add(lblDrift, 0, 2);
        innerTable.Controls.Add(lblSample, 0, 3);
        panel.Controls.Add(innerTable);
        return panel;
    }

    /// <summary>按钮面板 — 4行×2列 自适应大按钮</summary>
    private Panel CreateButtonPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.BgDark };

        var btnTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(2)
        };
        btnTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        btnTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        btnTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        btnTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        btnTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        btnTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

        btnNewTest     = MkBtn("新建试验", ThemeColors.AccentBlue);
        btnStartHeat   = MkBtn("开始升温", ThemeColors.AccentOrange);
        btnStopHeat    = MkBtn("停止升温", ThemeColors.AccentYellow);
        btnStartRecord = MkBtn("开始记录", ThemeColors.AccentGreen);
        btnStopRecord  = MkBtn("停止记录", ThemeColors.AccentYellow);
        btnTestRecord  = MkBtn("试验记录", ThemeColors.AccentPurple);
        btnSettings    = MkBtn("参数设置", ThemeColors.AccentDarkGray);

        btnTable.Controls.Add(btnNewTest, 0, 0);
        btnTable.SetColumnSpan(btnNewTest, 2);
        btnTable.Controls.Add(btnStartHeat, 0, 1);
        btnTable.Controls.Add(btnStopHeat, 1, 1);
        btnTable.Controls.Add(btnStartRecord, 0, 2);
        btnTable.Controls.Add(btnStopRecord, 1, 2);
        btnTable.Controls.Add(btnTestRecord, 0, 3);
        btnTable.Controls.Add(btnSettings, 1, 3);

        btnNewTest.Click     += (s, e) => OpenNewTestDialog();
        btnStartHeat.Click   += (s, e) => { if (_tc.StartHeating()) { _ctx.DaqWorker.Start(); UpdateButtonStates(); } };
        btnStopHeat.Click    += (s, e) => { if (_tc.StopHeating()) UpdateButtonStates(); };
        btnStartRecord.Click += (s, e) => { if (_tc.StartRecording()) UpdateButtonStates(); };
        btnStopRecord.Click  += (s, e) => { if (_tc.StopRecording()) UpdateButtonStates(); };
        btnTestRecord.Click  += (s, e) => OpenTestRecordDialog();
        btnSettings.Click    += (s, e) => { using var dlg = new SettingsForm(); dlg.ShowDialog(); };

        panel.Controls.Add(btnTable);
        return panel;
    }

    private Button MkBtn(string text, Color backColor)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Fill,
            Margin = new Padding(2),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = ControlPaint.Dark(backColor, 0.2f);
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.15f);
        btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.25f);
        return btn;
    }

    /// <summary>曲线开关面板 — 图表左侧</summary>
    private Panel CreateCurveLegendPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 110,
            BackColor = Color.FromArgb(248, 248, 248),
            Padding = new Padding(6, 8, 6, 8)
        };

        var lblTitle = new Label
        {
            Text = "曲线开关",
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            ForeColor = ThemeColors.TextSecondary,
            Location = new Point(6, 4),
            Size = new Size(95, 20)
        };

        cbTF1 = MkCurveCb("炉温1", ThemeColors.TempFurnace1, 28);
        cbTF2 = MkCurveCb("炉温2", ThemeColors.TempFurnace2, 52);
        cbTS  = MkCurveCb("表面温", ThemeColors.TempSurface, 76);
        cbTC  = MkCurveCb("中心温", ThemeColors.TempCenter, 100);

        panel.Controls.Add(lblTitle);
        panel.Controls.Add(cbTF1);
        panel.Controls.Add(cbTF2);
        panel.Controls.Add(cbTS);
        panel.Controls.Add(cbTC);
        return panel;
    }

    private CheckBox MkCurveCb(string text, Color color, int y)
    {
        var cb = new CheckBox
        {
            Text = text,
            ForeColor = color,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Location = new Point(8, y),
            Size = new Size(95, 20),
            Checked = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        return cb;
    }

    #endregion

    #region Plot (可拖拽缩放 + 悬停 ToolTip + 左侧曲线开关)

    private void SetupPlot()
    {
        plotModel = new PlotModel
        {
            Title = "温度曲线",
            TextColor = OxyColors.Black,
            PlotAreaBorderColor = OxyColors.Gray,
            IsLegendVisible = false  // 用左侧自定义开关代替
        };

        // X 轴（虚线网格）
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            Minimum = 0,
            Maximum = 600,
            TextColor = OxyColor.FromRgb(60, 60, 60),
            TitleColor = OxyColor.FromRgb(60, 60, 60),
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            MajorGridlineStyle = LineStyle.Dash,
            MajorGridlineColor = OxyColor.FromRgb(200, 200, 200),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(230, 230, 230)
        });

        // Y 轴（虚线网格），初始范围 0~800，运行时自动缩放
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            Minimum = 0,
            Maximum = 800,
            MinimumPadding = 0.05,
            MaximumPadding = 0.05,
            TextColor = OxyColor.FromRgb(60, 60, 60),
            TitleColor = OxyColor.FromRgb(60, 60, 60),
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            MajorGridlineStyle = LineStyle.Dash,
            MajorGridlineColor = OxyColor.FromRgb(200, 200, 200),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(230, 230, 230)
        });

        seriesTF1 = new LineSeries { Title = "炉温1", Color = OxyColors.Red, StrokeThickness = 2, MarkerType = MarkerType.None };
        seriesTF2 = new LineSeries { Title = "炉温2", Color = OxyColors.Orange, StrokeThickness = 2, MarkerType = MarkerType.None };
        seriesTS  = new LineSeries { Title = "表面温", Color = OxyColors.LimeGreen, StrokeThickness = 2, MarkerType = MarkerType.None };
        seriesTC  = new LineSeries { Title = "中心温", Color = OxyColors.SkyBlue, StrokeThickness = 2, MarkerType = MarkerType.None };

        plotModel.Series.Add(seriesTF1);
        plotModel.Series.Add(seriesTF2);
        plotModel.Series.Add(seriesTS);
        plotModel.Series.Add(seriesTC);

        plotView.Model = plotModel;

        // === 可拖拽 + 滚轮缩放 ===
        var controller = new PlotController();
        controller.UnbindAll();
        controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        controller.BindMouseDown(OxyMouseButton.Right, PlotCommands.ZoomRectangle);
        controller.Bind(new OxyMouseWheelGesture(), PlotCommands.ZoomWheel);
        plotView.Controller = controller;

        // === 悬停 ToolTip（只显示提示，不修改曲线，不干扰拖拽） ===
        plotView.MouseMove += PlotView_MouseMove;
        plotView.MouseLeave += (s, e) => { _chartTooltip.RemoveAll(); _chartTooltip.Active = false; };

        // === 左侧复选框：控制曲线显隐 ===
        cbTF1.CheckedChanged += (s, e) => seriesTF1.IsVisible = cbTF1.Checked;
        cbTF2.CheckedChanged += (s, e) => seriesTF2.IsVisible = cbTF2.Checked;
        cbTS.CheckedChanged  += (s, e) => seriesTS.IsVisible = cbTS.Checked;
        cbTC.CheckedChanged  += (s, e) => seriesTC.IsVisible = cbTC.Checked;
    }

    private void PlotView_MouseMove(object? sender, MouseEventArgs e)
    {
        if (plotView.ActualModel == null) return;

        var model = plotView.ActualModel;
        string bestTip = "";
        double bestDist = double.MaxValue;

        foreach (var series in model.Series.OfType<LineSeries>())
        {
            if (!series.IsVisible || series.Points.Count == 0) continue;

            // 用 OxyPlot 内置 HitTest，屏幕坐标直接传入，内部自动转换
            var hit = series.HitTest(new HitTestArguments(new ScreenPoint(e.X, e.Y), 12));
            if (hit == null) continue;

            int idx = (int)Math.Round(hit.Index);
            if (idx < 0 || idx >= series.Points.Count) continue;

            var pt = series.Points[idx];
            // 用像素距离排序，找最近的曲线（Transform 返回 plot-area 内坐标，加 offset 对齐屏幕坐标）
            var xAxis = series.XAxis ?? model.DefaultXAxis;
            var yAxis = series.YAxis ?? model.DefaultYAxis;
            if (xAxis == null || yAxis == null) continue;
            double sx = xAxis.Transform(pt.X) + model.PlotArea.Left;
            double sy = yAxis.Transform(pt.Y) + model.PlotArea.Top;
            double dist = (e.X - sx) * (e.X - sx) + (e.Y - sy) * (e.Y - sy);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestTip = $"{series.Title}\n━━━━━━\n时间: {pt.X:F0} s\n温度: {pt.Y:F1} °C";
            }
        }

        if (!string.IsNullOrEmpty(bestTip))
        {
            _chartTooltip.SetToolTip(plotView, bestTip);
            _chartTooltip.Active = true;
        }
        else
        {
            _chartTooltip.RemoveAll();
            _chartTooltip.Active = false;
        }
    }

    private void ClearChart()
    {
        seriesTF1.Points.Clear();
        seriesTF2.Points.Clear();
        seriesTS.Points.Clear();
        seriesTC.Points.Clear();
        plotModel.Axes[0].Minimum = 0;
        plotModel.Axes[0].Maximum = 600;
        plotModel.Axes[1].Minimum = 0;
        plotModel.Axes[1].Maximum = 800;
        _tickCounter = 0;
        plotModel.InvalidatePlot(true);
    }

    #endregion

    #region Query Tab

    private void BuildQueryTab()
    {
        // === 顶部筛选栏（标题 + 条件行） ===
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = ThemeColors.BgMedium,
            Padding = new Padding(14, 8, 14, 8)
        };

        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // 标题
        var titleBar = new Panel { Dock = DockStyle.Fill };
        titleBar.Controls.Add(new Label
        {
            Text = "▌ 记录查询",
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            Location = new Point(0, 0),
            Size = new Size(200, 24)
        });
        topLayout.Controls.Add(titleBar, 0, 0);

        // 筛选条件 + 按钮
        var filterRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0)
        };

        Control MkFilterLabel(string label, Control input, int w)
        {
            var p = new Panel { Size = new Size(62 + w, 38), Margin = new Padding(0, 0, 10, 0) };
            p.Controls.Add(new Label { Text = label, ForeColor = ThemeColors.TextMuted, Font = new Font("Microsoft YaHei", 8), Location = new Point(0, 0), Size = new Size(54, 14) });
            input.Location = new Point(0, 15);
            input.Size = new Size(w, 26);
            p.Controls.Add(input);
            return p;
        }

        dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddDays(-30), Font = new Font("Microsoft YaHei", 9) };
        dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Now, Font = new Font("Microsoft YaHei", 9) };
        filterRow.Controls.Add(MkFilterLabel("开始日期", dtpStart, 110));
        filterRow.Controls.Add(MkFilterLabel("结束日期", dtpEnd, 110));
        txtQueryProduct = new TextBox { Font = new Font("Microsoft YaHei", 9), BorderStyle = BorderStyle.FixedSingle };
        filterRow.Controls.Add(MkFilterLabel("样品编号", txtQueryProduct, 100));
        cmbQueryOperator = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Microsoft YaHei", 9), FlatStyle = FlatStyle.Flat };
        filterRow.Controls.Add(MkFilterLabel("操作员", cmbQueryOperator, 100));

        // 按钮组
        var btnBar = new Panel { Size = new Size(284, 38), Margin = new Padding(8, 0, 0, 0) };
        Button MkBtn(string t, Color c, int x, int w)
        {
            var b = new Button { Text = t, Location = new Point(x, 3), Size = new Size(w, 28), BackColor = c, ForeColor = Color.White, Font = new Font("Microsoft YaHei", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        var btnQuery = MkBtn("🔍 查询", ThemeColors.AccentBlue, 0, 72);
        var btnExport = MkBtn("📥 导出", ThemeColors.AccentGreen, 80, 78);
        var btnDelete = MkBtn("🗑 删除", ThemeColors.AccentRed, 166, 62);
        btnQuery.Click += (s, e) => RunQuery();
        btnExport.Click += (s, e) => ExportQuery();
        btnDelete.Click += (s, e) => DeleteRecord();
        btnBar.Controls.Add(btnQuery);
        btnBar.Controls.Add(btnExport);
        btnBar.Controls.Add(btnDelete);
        filterRow.Controls.Add(btnBar);

        topLayout.Controls.Add(filterRow, 0, 1);
        topPanel.Controls.Add(topLayout);

        // === 表格 ===
        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemeColors.BgDark,
            GridColor = Color.FromArgb(228, 230, 235),
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            BorderStyle = BorderStyle.None,
            RowTemplate = { Height = 34 },
            ColumnHeadersHeight = 40,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowHeadersWidth = 54,
            RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 242, 248),
                ForeColor = Color.FromArgb(55, 55, 68),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(0, 0, 0, 0)
            },
            RowHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 249, 252),
                ForeColor = Color.FromArgb(140, 142, 150),
                Font = new Font("Consolas", 8),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(35, 35, 48),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Padding = new Padding(10, 0, 10, 0)
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(246, 247, 252),
                ForeColor = Color.FromArgb(35, 35, 48),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Padding = new Padding(10, 0, 10, 0)
            }
        };

        // === 行号绘制 ===
        dgvRecords.RowPostPaint += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            var grid = (DataGridView)s!;
            var rect = new Rectangle(e.RowBounds.Left, e.RowBounds.Top,
                grid.RowHeadersWidth - 2, e.RowBounds.Height);
            var num = (e.RowIndex + 1).ToString();
            using var brush = new SolidBrush(Color.FromArgb(150, 152, 162));
            using var font = new Font("Consolas", 8.5f);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(num, font, brush, rect, sf);
            // 行号底部微细分隔线
            using var pen = new Pen(Color.FromArgb(228, 230, 235), 0.5f);
            e.Graphics.DrawLine(pen, e.RowBounds.Left, e.RowBounds.Bottom - 1,
                e.RowBounds.Left + grid.RowHeadersWidth, e.RowBounds.Bottom - 1);
        };
        // 隐藏默认行头文字
        dgvRecords.RowHeadersDefaultCellStyle.Padding = new Padding(0);

        // === 单元格格式化：数字右对齐、日期居中 ===
        dgvRecords.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Value == null) return;
            var col = dgvRecords.Columns[e.ColumnIndex];
            var name = col.HeaderText;
            // 数字列右对齐
            if (name is "试验前质量" or "试验后质量" or "失重率" or "温升" or "时长")
            {
                e.CellStyle!.Alignment = DataGridViewContentAlignment.MiddleRight;
                e.CellStyle.Padding = new Padding(0, 0, 14, 0);
            }
            // 日期列居中
            else if (name is "试验日期")
            {
                e.CellStyle!.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            // 文本列左对齐
            else
            {
                e.CellStyle!.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        };

        // === 悬停高亮 ===
        dgvRecords.CellMouseEnter += (s, e) =>
        {
            if (e.RowIndex >= 0)
                dgvRecords.Rows[e.RowIndex].DefaultCellStyle!.BackColor = Color.FromArgb(235, 240, 252);
        };
        dgvRecords.CellMouseLeave += (s, e) =>
        {
            if (e.RowIndex >= 0 && !dgvRecords.Rows[e.RowIndex].Selected)
            {
                bool alt = e.RowIndex % 2 == 1;
                dgvRecords.Rows[e.RowIndex].DefaultCellStyle!.BackColor =
                    alt ? Color.FromArgb(246, 247, 252) : Color.White;
            }
        };

        // === 底部统计栏 ===
        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Color.FromArgb(240, 242, 248),
            Padding = new Padding(12, 0, 12, 0)
        };
        lblRecordCount = new Label
        {
            Text = "共 0 条记录",
            ForeColor = Color.FromArgb(130, 132, 142),
            Font = new Font("Microsoft YaHei", 9),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        statusBar.Controls.Add(lblRecordCount);

        // 表格 + 统计栏包装
        var tablePanel = new Panel { Dock = DockStyle.Fill };
        tablePanel.Controls.Add(dgvRecords);
        tablePanel.Controls.Add(statusBar);

        // === 详情卡片面板 ===
        detailCardPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColors.BgMedium,
            Padding = new Padding(16, 14, 16, 14),
            AutoScroll = true
        };

        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = 430,
            BackColor = Color.White,
            Padding = new Padding(18, 14, 18, 14)
        };
        // 卡片边框
        card.Paint += (s, e) =>
        {
            var r = card.ClientRectangle; r.Width--; r.Height--;
            using var pen = new Pen(Color.FromArgb(215, 217, 224), 1);
            e.Graphics.DrawRectangle(pen, r);
        };

        lblDetailTitle = new Label
        {
            Text = "选中一条记录查看详情",
            ForeColor = ThemeColors.TextMuted,
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            Location = new Point(18, 14),
            Size = new Size(340, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        lblDetailVerdict = new Label
        {
            Text = "",
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 13, FontStyle.Bold),
            Location = new Point(18, 48),
            Size = new Size(340, 34),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        lblDetailBody = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(60, 60, 70),
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(18, 92),
            Size = new Size(340, 320),
            TextAlign = ContentAlignment.TopLeft
        };

        card.Controls.Add(lblDetailTitle);
        card.Controls.Add(lblDetailVerdict);
        card.Controls.Add(lblDetailBody);
        detailCardPanel.Controls.Add(card);

        dgvRecords.SelectionChanged += (s, e) => FillDetailCard();

        // === 拼装 ===
        var splitQuery = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 560,
            SplitterWidth = 4,
            BackColor = Color.FromArgb(210, 212, 218)
        };
        splitQuery.Panel1.Controls.Add(tablePanel);
        splitQuery.Panel2.Controls.Add(detailCardPanel);

        tabQuery.Controls.Add(splitQuery);
        tabQuery.Controls.Add(topPanel);

        // 填充操作员
        var ops = _ctx.Db.GetAllOperators();
        cmbQueryOperator.Items.Add("全部");
        foreach (var op in ops) cmbQueryOperator.Items.Add(op.Username);
        cmbQueryOperator.SelectedIndex = 0;
    }

    private void FillDetailCard()
    {
        if (dgvRecords.CurrentRow?.DataBoundItem == null)
        {
            lblDetailTitle.Text = "选中一条记录查看详情";
            lblDetailTitle.ForeColor = ThemeColors.TextMuted;
            lblDetailVerdict.Visible = false;
            lblDetailBody.Text = "";
            return;
        }
        dynamic row = dgvRecords.CurrentRow.DataBoundItem;
        var tm = _ctx.Db.GetTestMaster((string)row.样品编号, (string)row.试验标识);
        if (tm == null) return;

        bool pass = tm.DeltaTf <= 50 && tm.LostWeightPer <= 50 && tm.FlameDuration < 5;
        string verdict = pass ? "✓ 通过 — 材料判定为不燃" : "✗ 不通过";

        lblDetailTitle.Text = $"样品: {tm.ProductId}  |  试验: {tm.TestId}";
        lblDetailTitle.ForeColor = ThemeColors.TextPrimary;

        lblDetailVerdict.Text = verdict;
        lblDetailVerdict.BackColor = pass ? Color.FromArgb(46, 160, 67) : Color.FromArgb(220, 60, 50);
        lblDetailVerdict.Visible = true;

        lblDetailBody.Text =
            $"📋 基本信息\r\n" +
            $"   试验日期     {tm.TestDate}\r\n" +
            $"   操作员        {tm.Operator}\r\n" +
            $"\r\n" +
            $"🌡 环境参数\r\n" +
            $"   环境温度     {tm.EnvTemp:F1} °C\r\n" +
            $"   环境湿度     {tm.EnvHumidity:F1} %\r\n" +
            $"\r\n" +
            $"⚖ 质量数据\r\n" +
            $"   试验前质量  {tm.PreWeight:F2} g\r\n" +
            $"   试验后质量  {tm.PostWeight:F2} g\r\n" +
            $"   失重量        {tm.LostWeight:F2} g\r\n" +
            $"   失重率        {tm.LostWeightPer:F2} %\r\n" +
            $"\r\n" +
            $"🔥 温升数据\r\n" +
            $"   炉温1          {tm.DeltaTf1:F1} °C\r\n" +
            $"   炉温2          {tm.DeltaTf2:F1} °C\r\n" +
            $"   表面温度      {tm.DeltaTs:F1} °C\r\n" +
            $"   中心温度      {tm.DeltaTc:F1} °C\r\n" +
            $"   综合温升      {tm.DeltaTf:F1} °C\r\n" +
            $"\r\n" +
            $"⏱ 试验信息\r\n" +
            $"   火焰持续      {tm.FlameDuration} 秒\r\n" +
            $"   试验时长      {tm.TotalTestTime} 秒\r\n" +
            $"   备注           {(string.IsNullOrEmpty(tm.Remark) ? "—" : tm.Remark)}";
    }

    private static Button MkSmallBtn(string text, Color backColor, Point loc, Size size)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = size,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = ControlPaint.Dark(backColor, 0.2f);
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.15f);
        btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.25f);
        return btn;
    }

    private void RunQuery()
    {
        string? pid = string.IsNullOrWhiteSpace(txtQueryProduct.Text) ? null : txtQueryProduct.Text;
        string? op = cmbQueryOperator.SelectedIndex <= 0 ? null : cmbQueryOperator.SelectedItem?.ToString();
        string? sd = dtpStart.Value.ToString("yyyy-MM-dd");
        string? ed = dtpEnd.Value.AddDays(1).ToString("yyyy-MM-dd");
        var records = _ctx.Db.QueryTestMasters(pid, op, sd, ed);
        var list = records.Select(r => new {
            样品编号 = r.ProductId,
            试验标识 = r.TestId,
            试验日期 = r.TestDate,
            操作员 = r.Operator,
            试验前质量 = $"{r.PreWeight:F2} g",
            试验后质量 = $"{r.PostWeight:F2} g",
            失重率 = $"{r.LostWeightPer:F2}%",
            综合温升 = $"{r.DeltaTf:F1}°C",
            试验时长 = $"{r.TotalTestTime}s"
        }).ToList();
        dgvRecords.DataSource = list;
        lblRecordCount.Text = $"共 {list.Count} 条记录";
    }

    private void ExportQuery()
    {
        if (dgvRecords.Rows.Count == 0) { MessageBox.Show("无数据"); return; }
        using var sfd = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"查询_{DateTime.Now:yyyyMMddHHmmss}.xlsx" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            using var package = new OfficeOpenXml.ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("查询结果");
            for (int i = 0; i < dgvRecords.Columns.Count; i++)
                sheet.Cells[1, i + 1].Value = dgvRecords.Columns[i].HeaderText;
            for (int row = 0; row < dgvRecords.Rows.Count; row++)
                for (int col = 0; col < dgvRecords.Columns.Count; col++)
                    sheet.Cells[row + 2, col + 1].Value = dgvRecords.Rows[row].Cells[col].Value?.ToString() ?? "";
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(sfd.FileName));
            MessageBox.Show($"导出成功: {sfd.FileName}");
        }
    }

    private void DeleteRecord()
    {
        if (dgvRecords.CurrentRow?.DataBoundItem == null) { MessageBox.Show("请先选中一条记录"); return; }
        dynamic row = dgvRecords.CurrentRow.DataBoundItem;
        string pid = (string)row.ProductId;
        string tid = (string)row.TestId;
        if (MessageBox.Show($"确定删除试验 {pid}/{tid}？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            _ctx.Db.DeleteTestMaster(pid, tid);
            RunQuery();
        }
    }

    #endregion

    #region Calibration Tab

    private void BuildCalibrationTab()
    {
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = ThemeColors.BgLight };

        lblCalTemp = new Label { Text = "当前校准温: 0.0 °C", ForeColor = ThemeColors.CalGold, Font = new Font("Consolas", 20, FontStyle.Bold), Location = new Point(10, 15), Size = new Size(300, 40), TextAlign = ContentAlignment.MiddleCenter };
        topPanel.Controls.Add(new Label { Text = "标准温度(°C):", ForeColor = ThemeColors.TextPrimary, Location = new Point(320, 20), Size = new Size(100, 24) });
        txtCalRefTemp = new TextBox { Location = new Point(420, 18), Size = new Size(80, 24), Text = "750" };
        var btnCal = MkSmallBtn("记录校准", ThemeColors.AccentBlue, new Point(520, 15), new Size(100, 32));
        btnCal.Click += (s, e) =>
        {
            if (double.TryParse(txtCalRefTemp.Text, out double refTemp))
            {
                double measured = _ctx.DaqWorker.Temperatures["TCal"];
                _ctx.Db.InsertCalibrationRecord(new CalibrationRecord { CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Operator = _ctx.CurrentOperator, ReferenceTemp = refTemp, MeasuredTemp = measured, Deviation = measured - refTemp });
                LoadCalRecords();
                MessageBox.Show($"校准记录已保存\n偏差: {measured - refTemp:F1}°C");
            }
        };

        topPanel.Controls.Add(lblCalTemp);
        topPanel.Controls.Add(txtCalRefTemp);
        topPanel.Controls.Add(btnCal);

        dgvCalRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemeColors.BgDark,
            ForeColor = ThemeColors.TextPrimary,
            GridColor = ThemeColors.GridBorder,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = ThemeColors.GridHeader, ForeColor = ThemeColors.TextSecondary, Font = new Font("Microsoft YaHei", 9, FontStyle.Bold) },
            RowHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = ThemeColors.BgLight, ForeColor = ThemeColors.TextSecondary },
            DefaultCellStyle = new DataGridViewCellStyle { BackColor = ThemeColors.GridBg, ForeColor = ThemeColors.GridText, SelectionBackColor = ThemeColors.GridSelBg, SelectionForeColor = Color.White },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = ThemeColors.GridAltBg, ForeColor = ThemeColors.GridText, SelectionBackColor = ThemeColors.GridSelBg, SelectionForeColor = Color.White }
        };
        tabCalibration.Controls.Add(dgvCalRecords);
        tabCalibration.Controls.Add(topPanel);
        LoadCalRecords();
    }

    private void LoadCalRecords()
    {
        var records = _ctx.Db.GetCalibrationRecords();
        dgvCalRecords.DataSource = records.Select(r => new { r.Id, r.CalibrationDate, r.Operator, r.ReferenceTemp, r.MeasuredTemp, r.Deviation }).ToList();
    }

    #endregion

    #region Events

    private void WireEvents()
    {
        _ctx.DaqWorker.DataBroadcast += OnDataBroadcast;
        _tc.StateChanged += OnStateChanged;
    }

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        var temps = e.Temperatures;

        // === 防频闪：只在温度值实际变化时才更新 Label ===
        // 获取当前温度值
        double tf1 = temps["TF1"], tf2 = temps["TF2"], ts = temps["TS"], tc = temps["TC"], tcal = temps["TCal"];

        // 计算精度阈值（变化 < 0.05°C 时肉眼不可见，跳过更新）
        const double eps = 0.049;

        bool anyTempChanged = false;
        if (Math.Abs(tf1 - _lastTF1) > eps) { _lastTF1 = tf1; lblTF1Val.Text = $"{tf1:F1} °C"; anyTempChanged = true; }
        if (Math.Abs(tf2 - _lastTF2) > eps) { _lastTF2 = tf2; lblTF2Val.Text = $"{tf2:F1} °C"; anyTempChanged = true; }
        if (Math.Abs(ts - _lastTS) > eps)   { _lastTS = ts;   lblTSVal.Text = $"{ts:F1} °C";   anyTempChanged = true; }
        if (Math.Abs(tc - _lastTC) > eps)   { _lastTC = tc;   lblTCVal.Text = $"{tc:F1} °C";   anyTempChanged = true; }
        if (Math.Abs(tcal - _lastTCal) > eps) { _lastTCal = tcal; lblTCalVal.Text = $"{tcal:F1} °C"; lblCalTemp.Text = $"当前校准温: {tcal:F1} °C"; anyTempChanged = true; }

        // SuspendLayout 防止中间态闪烁
        if (anyTempChanged)
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        int runSec = _ctx.DaqWorker.TotalRunSeconds;
        string timerText;
        if (_tc.State == TestState.Recording)
            timerText = $"记录中: {e.ElapsedSeconds} 秒";
        else if (_tc.State == TestState.Complete && _tc.CurrentTest != null)
            timerText = $"已完成: {_tc.CurrentTest.TotalTestTime} 秒";
        else if (_tc.State == TestState.Preparing || _tc.State == TestState.Ready)
            timerText = $"运行: {runSec} 秒";
        else
            timerText = "计时: 0 秒";
        lblTimer.Text = timerText;

        if (!double.IsNaN(e.Drift)) lblDrift.Text = $"温漂: {e.Drift:F2} °C/10min";
        if (_tc.CurrentTest != null) lblSample.Text = $"样品: {_tc.CurrentTest.ProductId}";

        // 更新曲线数据（连续时间，不随记录开始而重置）
        double t = seriesTF1.Points.Count + 1;
        seriesTF1.Points.Add(new DataPoint(t, temps["TF1"]));
        seriesTF2.Points.Add(new DataPoint(t, temps["TF2"]));
        seriesTS.Points.Add(new DataPoint(t, temps["TS"]));
        seriesTC.Points.Add(new DataPoint(t, temps["TC"]));

        // X轴滚动（超过600秒后跟随数据滚动）
        bool axisChanged = false;
        if (t > 600)
        {
            plotModel.Axes[0].Minimum = t - 600;
            plotModel.Axes[0].Maximum = t;
            axisChanged = true;
        }

        foreach (var s in new[] { seriesTF1, seriesTF2, seriesTS, seriesTC })
            while (s.Points.Count > 800) s.Points.RemoveAt(0);

        _tickCounter++;

        // 防频闪：全刷新（重算Y轴范围）仅每 60 次 tick（约 48 秒）触发
        // 轻刷新（只更新曲线数据）每 tick 执行，保证曲线跟随数字同步变化
        bool fullRefresh = axisChanged || (_tickCounter % 60 == 0);

        // Y轴自动缩放：根据实际数据范围动态调整，避免曲线被压平
        if (fullRefresh)
        {
            double allMin = double.MaxValue, allMax = double.MinValue;
            foreach (var s in new[] { seriesTF1, seriesTF2, seriesTS, seriesTC })
            {
                if (!s.IsVisible || s.Points.Count < 2) continue;
                foreach (var pt in s.Points)
                {
                    if (pt.Y < allMin) allMin = pt.Y;
                    if (pt.Y > allMax) allMax = pt.Y;
                }
            }
            if (allMin < allMax)
            {
                double yRange = allMax - allMin;
                double yPad = Math.Max(yRange * 0.08, 10); // 上下各 8% 留白，至少 10°C
                double yAxisMin = Math.Max(0, allMin - yPad);
                double yAxisMax = allMax + yPad;
                // 防止范围太小导致图表抖动
                if (yAxisMax - yAxisMin < 40)
                { yAxisMin = Math.Max(0, yAxisMin - 20); yAxisMax = yAxisMin + 40; }
                plotModel.Axes[1].Minimum = yAxisMin;
                plotModel.Axes[1].Maximum = yAxisMax;
            }
        }

        // 每 tick 都刷新曲线数据（轻刷新），保证与数字同步；双缓冲已消除频闪
        plotModel.InvalidatePlot(fullRefresh);

        // 日志消息
        foreach (var msg in e.Messages)
        {
            Color color = msg.Message.Contains("终止") ? Color.OrangeRed :
                          msg.Message.Contains("错误") ? Color.Red : ThemeColors.TextSecondary;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText($"{msg.Time}  {msg.Message}\n");
            rtbLog.ScrollToCaret();
        }

        _tc.DoWork();
    }

    private void OnStateChanged(object? sender, string state)
    {
        lblStatus.Text = state switch { "Idle" => "空闲", "Preparing" => "升温中", "Ready" => "就绪", "Recording" => "记录中", "Complete" => "完成", _ => state };
        lblStatus.ForeColor = state switch
        {
            "Idle" => Color.Gray,
            "Preparing" => Color.Orange,
            "Ready" => Color.LimeGreen,
            "Recording" => Color.Red,
            "Complete" => Color.DodgerBlue,
            _ => Color.White
        };
        UpdateButtonStates();
    }

    #endregion

    #region Button states & dialogs

    private void UpdateButtonStates()
    {
        var s = _tc.State;
        bool hasUnSaved = _tc.HasUnSavedCompleteTest();
        bool hasActive = _tc.CurrentTest != null;

        btnNewTest.Enabled = s == TestState.Idle || (s == TestState.Preparing && !hasActive) || (s == TestState.Complete && !hasUnSaved);
        btnStartHeat.Enabled = s == TestState.Idle;
        btnStopHeat.Enabled = s == TestState.Preparing || s == TestState.Ready || s == TestState.Complete;
        btnStartRecord.Enabled = s == TestState.Ready && !hasUnSaved && _tc.CurrentTest != null;
        btnStopRecord.Enabled = s == TestState.Recording;
        btnTestRecord.Enabled = hasUnSaved;
        btnSettings.Enabled = s != TestState.Recording;
    }

    private void OpenNewTestDialog()
    {
        using var dlg = new NewTestForm();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _tc.CreateTest(dlg.TestMaster!, dlg.ProductMaster!);
            ClearChart();
            if (_tc.State == TestState.Idle)
            {
                _tc.StartHeating();
                _ctx.DaqWorker.Start();
            }
            UpdateButtonStates();
        }
    }

    private void OpenTestRecordDialog()
    {
        if (_tc.CurrentTest == null) return;
        using var dlg = new TestRecordForm(_tc.CurrentTest);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            // 1. 保存试验记录到数据库（核心操作）
            try
            {
                _tc.SaveTestRecord(dlg.PostWeight, dlg.HasFlame ? 1 : 0, dlg.FlameStartTime, dlg.FlameDuration, dlg.Remark);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _tc.ClearCurrentTest();
                UpdateButtonStates();
                return;
            }

            var tm = _tc.CurrentTest;
            var tempData = _tc.TemperatureHistory;

            // 2. 保存温度数据到数据库
            try
            {
                if (tempData.Count > 0)
                    _ctx.Db.InsertTemperatureData(tm.ProductId, tm.TestId, tempData);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "温度数据保存失败: {ProductId}/{TestId}", tm.ProductId, tm.TestId);
            }

            // 3. 导出文件（每个导出独立，互不影响）
            var exportErrors = new List<string>();
            try { _ctx.ExportService.ExportCsv(tm, tempData); }
            catch (Exception ex) { exportErrors.Add($"CSV: {ex.Message}"); Log.Warning(ex, "CSV导出失败"); }

            try { _ctx.ExportService.ExportExcel(tm, tempData); }
            catch (Exception ex) { exportErrors.Add($"Excel: {ex.Message}"); Log.Warning(ex, "Excel导出失败"); }

            if (bool.TryParse(_ctx.Configuration["Report:EnablePdfExport"], out bool enablePdf) && enablePdf)
            {
                try { _ctx.ExportService.ExportPdf(tm, tempData); }
                catch (Exception ex) { exportErrors.Add($"PDF: {ex.Message}"); Log.Warning(ex, "PDF导出失败"); }
            }

            // 4. 显示结果
            if (exportErrors.Count > 0)
            {
                MessageBox.Show(
                    $"试验记录已保存。\n\n以下报告生成失败:\n• {string.Join("\n• ", exportErrors)}",
                    "部分完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("试验记录已保存，报告已生成。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // 5. 自动刷新查询并跳转
            RunQuery();
            tabControl.SelectedTab = tabQuery;

            _tc.ClearCurrentTest();
            UpdateButtonStates();
        }
    }

    #endregion

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _ctx.DaqWorker.Stop();
        _ctx.DaqWorker.Dispose();
        base.OnFormClosing(e);
    }
}
