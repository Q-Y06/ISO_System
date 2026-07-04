using ISO11820.Core;
using ISO11820.Helpers;
using ISO11820.Models;
using ISO11820.Services;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.Forms;

public partial class MainForm : Form
{
    private readonly AppContext _ctx = AppContext.Instance;
    private readonly TestController _tc;

    // Temperature display labels
    private Label lblTF1Val = null!, lblTF2Val = null!, lblTSVal = null!, lblTCVal = null!, lblTCalVal = null!;
    private Label lblStatus = null!, lblTimer = null!, lblDrift = null!, lblSample = null!;

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

    // Calibration tab
    private Label lblCalTemp = null!;
    private TextBox txtCalRefTemp = null!;
    private DataGridView dgvCalRecords = null!;

    public MainForm()
    {
        // 启用全窗体双缓冲，消除控件重绘导致的频闪
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

        _tc = _ctx.TestController;
        InitializeComponent();
        SetupPlot();
        WireEvents();
        ShowInitialTemperatures();
        UpdateButtonStates();

        // 切换到"记录查询"tab时自动加载数据
        tabControl.SelectedIndexChanged += (s, e) =>
        {
            if (tabControl.SelectedTab == tabQuery)
                RunQuery();
        };

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

        // 启用双缓冲防止布局面板重绘闪烁
        typeof(TableLayoutPanel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, ledTable, new object[] { true });

        for (int i = 0; i < 5; i++)
        {
            var ledPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.BgLed,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3)
            };
            // 启用双缓冲防止温度数值更新时频闪
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, ledPanel, new object[] { true });

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

        // Y 轴（虚线网格）
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            Minimum = 0,
            Maximum = 800,
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
        // 修正：减去图表区域偏移，得到 plot-area 内的坐标
        double px = e.X - model.PlotArea.Left;
        double py = e.Y - model.PlotArea.Top;

        // 用轴直接反算数据坐标，再找最近点（不受 zoom/pan 影响）
        double bestDist = double.MaxValue;
        string bestTip = "";

        foreach (var series in model.Series.OfType<LineSeries>())
        {
            if (!series.IsVisible || series.Points.Count == 0) continue;

            var xAxis = series.XAxis ?? model.DefaultXAxis;
            var yAxis = series.YAxis ?? model.DefaultYAxis;
            if (xAxis == null || yAxis == null) continue;

            // 屏幕坐标 → 数据坐标
            double dataX = xAxis.InverseTransform(px);
            double dataY = yAxis.InverseTransform(py);

            // 在数据空间找最近点
            double minDx = (xAxis.ActualMaximum - xAxis.ActualMinimum) / 400; // ~1.5s 容差
            double minDy = (yAxis.ActualMaximum - yAxis.ActualMinimum) / 250; // ~3°C 容差
            double bestIdxDist = double.MaxValue;

            for (int i = 0; i < series.Points.Count; i++)
            {
                var pt = series.Points[i];
                double ddx = (pt.X - dataX) / minDx;
                double ddy = (pt.Y - dataY) / minDy;
                double ndist = ddx * ddx + ddy * ddy;
                if (ndist < bestIdxDist) bestIdxDist = ndist;
            }

            if (bestIdxDist < 1.0 && bestIdxDist < bestDist)
            {
                bestDist = bestIdxDist;
                // 找到最近点后再查一次
                for (int i = 0; i < series.Points.Count; i++)
                {
                    var pt = series.Points[i];
                    double ddx = (pt.X - dataX) / minDx;
                    double ddy = (pt.Y - dataY) / minDy;
                    double ndist = ddx * ddx + ddy * ddy;
                    if (Math.Abs(ndist - bestIdxDist) < 0.001)
                    {
                        bestTip = $"{series.Title}\n━━━━━━\n时间: {pt.X:F0} s\n温度: {pt.Y:F1} °C";
                        break;
                    }
                }
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
        _tickCounter = 0;
        plotModel.InvalidatePlot(true);
    }

    #endregion

    #region Query Tab

    private void BuildQueryTab()
    {
        // === 顶部标题栏 ===
        var titleBar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = ThemeColors.BgMedium };
        titleBar.Controls.Add(new Label
        {
            Text = "▌ 试验记录查询",
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            Location = new Point(12, 7),
            Size = new Size(200, 24)
        });

        // === 筛选栏 — 分组卡片式 ===
        var filterBar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = ThemeColors.BgDark, Padding = new Padding(8, 6, 8, 6) };

        // 日期范围组
        var lblDate = new Label { Text = "日期范围:", ForeColor = ThemeColors.TextMuted, Font = new Font("Microsoft YaHei", 9), Location = new Point(8, 18), Size = new Size(62, 22), TextAlign = ContentAlignment.MiddleRight };
        dtpStart = new DateTimePicker { Location = new Point(72, 16), Size = new Size(115, 24), Format = DateTimePickerFormat.Short };
        var lblSep = new Label { Text = "~", ForeColor = ThemeColors.TextMuted, Font = new Font("Microsoft YaHei", 9), Location = new Point(190, 18), Size = new Size(18, 22), TextAlign = ContentAlignment.MiddleCenter };
        dtpEnd = new DateTimePicker { Location = new Point(210, 16), Size = new Size(115, 24), Format = DateTimePickerFormat.Short };

        // 样品编号组
        var lblProduct = new Label { Text = "样品编号:", ForeColor = ThemeColors.TextMuted, Font = new Font("Microsoft YaHei", 9), Location = new Point(340, 18), Size = new Size(62, 22), TextAlign = ContentAlignment.MiddleRight };
        txtQueryProduct = new TextBox { Location = new Point(404, 16), Size = new Size(110, 24), Font = new Font("Microsoft YaHei", 9), BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

        // 操作员组
        var lblOp = new Label { Text = "操作员:", ForeColor = ThemeColors.TextMuted, Font = new Font("Microsoft YaHei", 9), Location = new Point(526, 18), Size = new Size(52, 22), TextAlign = ContentAlignment.MiddleRight };
        cmbQueryOperator = new ComboBox { Location = new Point(580, 16), Size = new Size(100, 24), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9) };

        // 操作按钮组
        var btnQuery = MkSmallBtn("🔍 查询", ThemeColors.AccentBlue, new Point(698, 12), new Size(88, 34));
        btnQuery.Click += (s, e) => RunQuery();
        var btnExport = MkSmallBtn("📥 导出", ThemeColors.AccentGreen, new Point(794, 12), new Size(88, 34));
        btnExport.Click += (s, e) => ExportQuery();
        var btnDelete = MkSmallBtn("🗑 删除", ThemeColors.AccentRed, new Point(890, 12), new Size(75, 34));
        btnDelete.Click += (s, e) => DeleteRecord();

        filterBar.Controls.Add(lblDate);
        filterBar.Controls.Add(dtpStart);
        filterBar.Controls.Add(lblSep);
        filterBar.Controls.Add(dtpEnd);
        filterBar.Controls.Add(lblProduct);
        filterBar.Controls.Add(txtQueryProduct);
        filterBar.Controls.Add(lblOp);
        filterBar.Controls.Add(cmbQueryOperator);
        filterBar.Controls.Add(btnQuery);
        filterBar.Controls.Add(btnExport);
        filterBar.Controls.Add(btnDelete);

        // === 结果统计栏 ===
        var lblResultCount = new Label
        {
            Text = "共 0 条记录",
            ForeColor = ThemeColors.TextSecondary,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        var resultBar = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = ThemeColors.BgLight };
        resultBar.Controls.Add(lblResultCount);

        // 记录 DataGridView 后，动态更新统计数
        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemeColors.BgDark,
            ForeColor = ThemeColors.TextPrimary,
            GridColor = Color.FromArgb(230, 230, 230),
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 32 },
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeight = 36,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(0, 0, 0, 0)
            },
            RowHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(140, 140, 140),
                Font = new Font("Consolas", 8),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(4, 0, 4, 0)
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 250, 252),
                ForeColor = Color.FromArgb(40, 40, 40),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(4, 0, 4, 0)
            }
        };
        dgvRecords.DataSourceChanged += (s, e) =>
        {
            lblResultCount.Text = $"共 {dgvRecords.RowCount} 条记录";
        };

        // === 详情面板 — 卡片式布局 ===
        var detailPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.BgDark, Padding = new Padding(8) };

        var detailCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeColors.BgMedium,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12)
        };

        var detailTitle = new Label
        {
            Text = "📋 试验详情",
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30
        };

        var txtDetail = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(50, 50, 50),
            Font = new Font("Microsoft YaHei", 10),
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical,
            Text = "← 选中一条记录查看详情"
        };

        detailCard.Controls.Add(txtDetail);
        detailCard.Controls.Add(detailTitle);
        detailPanel.Controls.Add(detailCard);

        dgvRecords.SelectionChanged += (s, e) =>
        {
            if (dgvRecords.CurrentRow?.DataBoundItem == null) return;
            dynamic row = dgvRecords.CurrentRow.DataBoundItem;
            var tm = _ctx.Db.GetTestMaster((string)row.ProductId, (string)row.TestId);
            if (tm != null)
            {
                string verdict = (tm.DeltaTf <= 50 && tm.LostWeightPer <= 50 && tm.FlameDuration < 5)
                    ? "✅ 通过 — 材料判定为不燃" : "❌ 不通过";
                string verdictColor = verdict.StartsWith("✅") ? "通过" : "不通过";
                txtDetail.Text =
                    $"═══════════════════════════════\r\n" +
                    $"  样品编号     {tm.ProductId}\r\n" +
                    $"  试验标识     {tm.TestId}\r\n" +
                    $"  试验日期     {tm.TestDate}\r\n" +
                    $"  操 作 员     {tm.Operator}\r\n" +
                    $"═══════════════════════════════\r\n" +
                    $"  环境温度     {tm.EnvTemp:F1} °C\r\n" +
                    $"  环境湿度     {tm.EnvHumidity:F1} %\r\n" +
                    $"═══════════════════════════════\r\n" +
                    $"  试验前质量   {tm.PreWeight:F2} g\r\n" +
                    $"  试验后质量   {tm.PostWeight:F2} g\r\n" +
                    $"  失 重 量     {tm.LostWeight:F2} g\r\n" +
                    $"  失 重 率     {tm.LostWeightPer:F2} %\r\n" +
                    $"═══════════════════════════════\r\n" +
                    $"  炉温1 温升   {tm.DeltaTf1:F1} °C\r\n" +
                    $"  炉温2 温升   {tm.DeltaTf2:F1} °C\r\n" +
                    $"  表面温升     {tm.DeltaTs:F1} °C\r\n" +
                    $"  中心温升     {tm.DeltaTc:F1} °C\r\n" +
                    $"  综合温升     {tm.DeltaTf:F1} °C\r\n" +
                    $"═══════════════════════════════\r\n" +
                    $"  火焰持续     {tm.FlameDuration} 秒\r\n" +
                    $"  试验时长     {tm.TotalTestTime} 秒\r\n" +
                    $"  备注信息     {tm.Remark}\r\n" +
                    $"═══════════════════════════════\r\n" +
                    $"  判定结论  ▶  {verdict}";
            }
        };

        // === 左右分割布局 ===
        var splitQuery = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 580,
            SplitterWidth = 4,
            BackColor = Color.FromArgb(220, 220, 220)
        };

        var leftPanel = new Panel { Dock = DockStyle.Fill };
        leftPanel.Controls.Add(dgvRecords);
        leftPanel.Controls.Add(resultBar);

        splitQuery.Panel1.Controls.Add(leftPanel);
        splitQuery.Panel2.Controls.Add(detailPanel);

        tabQuery.Controls.Add(splitQuery);
        tabQuery.Controls.Add(filterBar);
        tabQuery.Controls.Add(titleBar);

        var ops = _ctx.Db.GetAllOperators();
        cmbQueryOperator.Items.Add("全部");
        foreach (var op in ops) cmbQueryOperator.Items.Add(op.Username);
        cmbQueryOperator.SelectedIndex = 0;
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
        dgvRecords.DataSource = records.Select(r => new {
            r.ProductId, r.TestId, r.TestDate, r.Operator,
            r.PreWeight, r.PostWeight,
            失重率 = $"{r.LostWeightPer:F2}%",
            温升 = $"{r.DeltaTf:F1}°C",
            时长 = $"{r.TotalTestTime}s"
        }).ToList();
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

        // 批量更新温度标签，减少重绘次数
        this.SuspendLayout();
        lblTF1Val.Text = $"{temps["TF1"]:F1} °C";
        lblTF2Val.Text = $"{temps["TF2"]:F1} °C";
        lblTSVal.Text = $"{temps["TS"]:F1} °C";
        lblTCVal.Text = $"{temps["TC"]:F1} °C";
        lblTCalVal.Text = $"{temps["TCal"]:F1} °C";
        lblCalTemp.Text = $"当前校准温: {temps["TCal"]:F1} °C";

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

        // 更新曲线数据
        double t = _tc.State == TestState.Recording ? e.ElapsedSeconds : seriesTF1.Points.Count + 1;
        seriesTF1.Points.Add(new DataPoint(t, temps["TF1"]));
        seriesTF2.Points.Add(new DataPoint(t, temps["TF2"]));
        seriesTS.Points.Add(new DataPoint(t, temps["TS"]));
        seriesTC.Points.Add(new DataPoint(t, temps["TC"]));

        bool axisChanged = false;
        if (t > 600)
        {
            plotModel.Axes[0].Minimum = t - 600;
            plotModel.Axes[0].Maximum = t;
            axisChanged = true;
        }

        foreach (var s in new[] { seriesTF1, seriesTF2, seriesTS, seriesTC })
            while (s.Points.Count > 800) s.Points.RemoveAt(0);

        // 轴变了或每60次tick才全刷新，否则只重绘（流畅不卡）
        _tickCounter++;
        bool fullRefresh = axisChanged || (_tickCounter % 60 == 0);
        plotModel.InvalidatePlot(fullRefresh);

        this.ResumeLayout(false);

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
            _tc.SaveTestRecord(dlg.PostWeight, dlg.HasFlame ? 1 : 0, dlg.FlameStartTime, dlg.FlameDuration, dlg.Remark);
            var tm = _tc.CurrentTest;
            var tempData = _tc.TemperatureHistory;

            var exportErrors = new List<string>();
            try
            {
                _ctx.ExportService.ExportCsv(tm, tempData);
            }
            catch (Exception ex) { exportErrors.Add($"CSV: {ex.Message}"); }

            try
            {
                _ctx.ExportService.ExportExcel(tm, tempData);
            }
            catch (Exception ex) { exportErrors.Add($"Excel: {ex.Message}"); }

            if (bool.TryParse(_ctx.Configuration["Report:EnablePdfExport"], out bool enablePdf) && enablePdf)
            {
                try
                {
                    _ctx.ExportService.ExportPdf(tm, tempData);
                }
                catch (Exception ex) { exportErrors.Add($"PDF: {ex.Message}"); }
            }

            if (exportErrors.Count > 0)
                MessageBox.Show($"试验记录已保存，但以下报告导出失败:\n\n{string.Join("\n", exportErrors)}",
                    "部分成功", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show("试验记录已保存，报告已生成。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _tc.PrepareForNextRecording();
            UpdateButtonStates();
            // 保存后刷新查询结果（若当前在查询tab则自动刷新）
            if (tabControl.SelectedTab == tabQuery)
                RunQuery();
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
