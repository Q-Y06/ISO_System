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
        _tc = _ctx.TestController;
        InitializeComponent();
        SetupPlot();
        WireEvents();
        ShowInitialTemperatures();
        UpdateButtonStates();
        // 显示系统初始化消息
        this.Load += (s, e) =>
        {
            rtbLog.SelectionColor = Color.White;
            rtbLog.AppendText($"{DateTime.Now:HH:mm:ss}  系统初始化，操作员：{_ctx.CurrentOperator}\n");
            rtbLog.ScrollToCaret();
        };
    }

    private void ShowInitialTemperatures()
    {
        // 显示初始温度但不启动仿真（等用户点击"开始升温"时才启动）
        var temps = _ctx.DaqWorker.Temperatures;
        lblTF1Val.Text = $"{temps["TF1"]:F1} °C";
        lblTF2Val.Text = $"{temps["TF2"]:F1} °C";
        lblTSVal.Text = $"{temps["TS"]:F1} °C";
        lblTCVal.Text = $"{temps["TC"]:F1} °C";
        lblTCalVal.Text = $"{temps["TCal"]:F1} °C";
        lblCalTemp.Text = $"当前校准温: {temps["TCal"]:F1} °C";
    }

    #region UI Setup

    private void InitializeComponent()
    {
        this.Text = "ISO 11820 建筑材料不燃性试验系统";
        this.Size = new Size(1280, 820);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1024, 700);
        this.BackColor = ThemeColors.BgDark;
        this.ForeColor = ThemeColors.TextPrimary;

        tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei", 10) };

        // --- Tab 1: Main ---
        tabMain = new TabPage("试验控制");
        tabMain.BackColor = ThemeColors.BgDark;

        var tempPanel = CreateTemperaturePanel();
        var statusPanel = CreateStatusPanel();
        var buttonPanel = CreateButtonPanel();

        plotView = new PlotView { Dock = DockStyle.Fill, BackColor = ThemeColors.BgDark };

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

        // 日志标题栏
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

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = ThemeColors.BgDark };
        topPanel.Controls.Add(tempPanel);
        topPanel.Controls.Add(statusPanel);
        topPanel.Controls.Add(buttonPanel);

        var splitCenter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 320
        };
        splitCenter.Panel1.Controls.Add(plotView);
        splitCenter.Panel2.Controls.Add(rtbLog);
        splitCenter.Panel2.Controls.Add(logTitlePanel);

        var centerPanel = new Panel { Dock = DockStyle.Fill };
        centerPanel.Controls.Add(splitCenter);

        tabMain.Controls.Add(centerPanel);
        tabMain.Controls.Add(topPanel);

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

    private Panel CreateTemperaturePanel()
    {
        var panel = new Panel { Location = new Point(10, 10), Size = new Size(750, 130), BackColor = ThemeColors.BgDark };
        var labels = new[] { "炉温1", "炉温2", "表面温", "中心温", "校准温" };
        var colors = new[] { ThemeColors.TempFurnace1, ThemeColors.TempFurnace2, ThemeColors.TempSurface, ThemeColors.TempCenter, ThemeColors.TempCal };
        var lbls = new Label[5];

        for (int i = 0; i < 5; i++)
        {
            int xPos = 10 + i * 145;
            // LED 背景面板
            var ledPanel = new Panel
            {
                Location = new Point(xPos, 0),
                Size = new Size(135, 125),
                BackColor = ThemeColors.BgLed,
                BorderStyle = BorderStyle.FixedSingle
            };
            // 通道名称
            var lblName = new Label
            {
                Text = labels[i],
                ForeColor = ThemeColors.TextMuted,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                Location = new Point(0, 5),
                Size = new Size(133, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            // 温度数值（LED 风格）
            lbls[i] = new Label
            {
                Text = "0.0 °C",
                ForeColor = colors[i],
                BackColor = ThemeColors.BgLedValue,
                Font = new Font("Consolas", 24, FontStyle.Bold),
                Location = new Point(5, 30),
                Size = new Size(123, 45),
                TextAlign = ContentAlignment.MiddleCenter
            };
            // 通道编号
            var lblChannel = new Label
            {
                Text = $"CH{i + 1}",
                ForeColor = ThemeColors.TextGray,
                Font = new Font("Consolas", 8),
                Location = new Point(0, 80),
                Size = new Size(133, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };
            ledPanel.Controls.Add(lblName);
            ledPanel.Controls.Add(lbls[i]);
            ledPanel.Controls.Add(lblChannel);
            panel.Controls.Add(ledPanel);
        }

        lblTF1Val = lbls[0]; lblTF2Val = lbls[1]; lblTSVal = lbls[2]; lblTCVal = lbls[3]; lblTCalVal = lbls[4];
        return panel;
    }

    private Panel CreateStatusPanel()
    {
        var panel = new Panel { Location = new Point(770, 10), Size = new Size(240, 130), BackColor = ThemeColors.BgMedium };
        lblStatus = new Label { Text = "空闲", ForeColor = ThemeColors.TextPrimary, Font = new Font("Microsoft YaHei", 14, FontStyle.Bold), Location = new Point(10, 10), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleCenter };
        lblTimer = new Label { Text = "计时: 0 秒", ForeColor = ThemeColors.StatusCyan, Font = new Font("Consolas", 18, FontStyle.Bold), Location = new Point(10, 45), Size = new Size(220, 35), TextAlign = ContentAlignment.MiddleCenter };
        lblDrift = new Label { Text = "温漂: -- °C/10min", ForeColor = ThemeColors.TextSecondary, Font = new Font("Microsoft YaHei", 10), Location = new Point(10, 85), Size = new Size(220, 20), TextAlign = ContentAlignment.MiddleCenter };
        lblSample = new Label { Text = "样品: --", ForeColor = ThemeColors.TextSecondary, Font = new Font("Microsoft YaHei", 9), Location = new Point(10, 107), Size = new Size(220, 18), TextAlign = ContentAlignment.MiddleCenter };
        panel.Controls.Add(lblStatus);
        panel.Controls.Add(lblTimer);
        panel.Controls.Add(lblDrift);
        panel.Controls.Add(lblSample);
        return panel;
    }

    private Panel CreateButtonPanel()
    {
        var panel = new Panel { Location = new Point(1020, 10), Size = new Size(220, 130), BackColor = ThemeColors.BgDark };

        btnNewTest = MkBtn("新建试验", new Point(5, 5), ThemeColors.AccentBlue);
        btnStartHeat = MkBtn("开始升温", new Point(5, 42), ThemeColors.AccentOrange);
        btnStopHeat = MkBtn("停止升温", new Point(115, 42), ThemeColors.AccentYellow);
        btnStartRecord = MkBtn("开始记录", new Point(5, 79), ThemeColors.AccentGreen);
        btnStopRecord = MkBtn("停止记录", new Point(115, 79), ThemeColors.AccentYellow);
        btnTestRecord = MkBtn("试验记录", new Point(5, 116), ThemeColors.AccentPurple);
        btnSettings = MkBtn("参数设置", new Point(115, 116), ThemeColors.AccentDarkGray);

        btnNewTest.Click += (s, e) => OpenNewTestDialog();
        btnStartHeat.Click += (s, e) => { if (_tc.StartHeating()) { _ctx.DaqWorker.Start(); UpdateButtonStates(); } };
        btnStopHeat.Click += (s, e) => { if (_tc.StopHeating()) UpdateButtonStates(); };
        btnStartRecord.Click += (s, e) => { if (_tc.StartRecording()) UpdateButtonStates(); };
        btnStopRecord.Click += (s, e) => { if (_tc.StopRecording()) UpdateButtonStates(); };
        btnTestRecord.Click += (s, e) => OpenTestRecordDialog();
        btnSettings.Click += (s, e) => { using var dlg = new SettingsForm(); dlg.ShowDialog(); };

        panel.Controls.AddRange(new Control[] { btnNewTest, btnStartHeat, btnStopHeat, btnStartRecord, btnStopRecord, btnTestRecord, btnSettings });
        return panel;
    }

    private Button MkBtn(string text, Point loc, Color backColor)
    {
        var btn = new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(105, 30),
            Font = new Font("Microsoft YaHei", 9),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor);
        btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.2f);
        return btn;
    }

    #endregion

    #region Plot

    private void SetupPlot()
    {
        plotModel = new PlotModel
        {
            Title = "温度曲线",
            TextColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            IsLegendVisible = true
        };
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "时间 (秒)", Minimum = 0, Maximum = 600, TextColor = OxyColors.White, TitleColor = OxyColors.White, AxislineColor = OxyColors.Gray, TicklineColor = OxyColors.Gray });
        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "温度 (°C)", Minimum = 0, Maximum = 800, TextColor = OxyColors.White, TitleColor = OxyColors.White, AxislineColor = OxyColors.Gray, TicklineColor = OxyColors.Gray });

        seriesTF1 = new LineSeries { Title = "炉温1", Color = OxyColors.Red, StrokeThickness = 1.5, MarkerType = MarkerType.None };
        seriesTF2 = new LineSeries { Title = "炉温2", Color = OxyColors.Orange, StrokeThickness = 1.5, MarkerType = MarkerType.None };
        seriesTS = new LineSeries { Title = "表面温", Color = OxyColors.LimeGreen, StrokeThickness = 1.5, MarkerType = MarkerType.None };
        seriesTC = new LineSeries { Title = "中心温", Color = OxyColors.SkyBlue, StrokeThickness = 1.5, MarkerType = MarkerType.None };

        plotModel.Series.Add(seriesTF1);
        plotModel.Series.Add(seriesTF2);
        plotModel.Series.Add(seriesTS);
        plotModel.Series.Add(seriesTC);

        plotView.Model = plotModel;
    }

    private void ClearChart()
    {
        seriesTF1.Points.Clear();
        seriesTF2.Points.Clear();
        seriesTS.Points.Clear();
        seriesTC.Points.Clear();
        plotModel.Axes[0].Minimum = 0;
        plotModel.Axes[0].Maximum = 600;
        plotModel.InvalidatePlot(true);
    }

    #endregion

    #region Query Tab

    private void BuildQueryTab()
    {
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = ThemeColors.BgLight };

        topPanel.Controls.Add(new Label { Text = "开始:", ForeColor = ThemeColors.TextPrimary, Location = new Point(10, 15), Size = new Size(40, 22) });
        dtpStart = new DateTimePicker { Location = new Point(50, 12), Size = new Size(120, 24), Format = DateTimePickerFormat.Short };
        topPanel.Controls.Add(new Label { Text = "结束:", ForeColor = ThemeColors.TextPrimary, Location = new Point(180, 15), Size = new Size(40, 22) });
        dtpEnd = new DateTimePicker { Location = new Point(220, 12), Size = new Size(120, 24), Format = DateTimePickerFormat.Short };
        topPanel.Controls.Add(new Label { Text = "样品:", ForeColor = ThemeColors.TextPrimary, Location = new Point(350, 15), Size = new Size(40, 22) });
        txtQueryProduct = new TextBox { Location = new Point(390, 12), Size = new Size(100, 24) };
        topPanel.Controls.Add(new Label { Text = "操作员:", ForeColor = ThemeColors.TextPrimary, Location = new Point(500, 15), Size = new Size(55, 22) });
        cmbQueryOperator = new ComboBox { Location = new Point(555, 12), Size = new Size(100, 24), DropDownStyle = ComboBoxStyle.DropDownList };

        var btnQuery = new Button { Text = "查询", Location = new Point(670, 10), Size = new Size(80, 30), BackColor = ThemeColors.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnQuery.Click += (s, e) => RunQuery();
        var btnExport = new Button { Text = "导出CSV", Location = new Point(760, 10), Size = new Size(90, 30), BackColor = ThemeColors.AccentGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnExport.Click += (s, e) => ExportQuery();
        var btnDelete = new Button { Text = "删除", Location = new Point(860, 10), Size = new Size(60, 30), BackColor = ThemeColors.AccentRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnDelete.Click += (s, e) => DeleteRecord();

        topPanel.Controls.Add(dtpStart);
        topPanel.Controls.Add(dtpEnd);
        topPanel.Controls.Add(txtQueryProduct);
        topPanel.Controls.Add(cmbQueryOperator);
        topPanel.Controls.Add(btnQuery);
        topPanel.Controls.Add(btnExport);
        topPanel.Controls.Add(btnDelete);

        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemeColors.BgDark,
            ForeColor = ThemeColors.TextPrimary,
            GridColor = ThemeColors.GridBorder,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeColors.GridHeader,
                ForeColor = ThemeColors.TextSecondary,
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold)
            },
            RowHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeColors.BgLight,
                ForeColor = ThemeColors.TextSecondary
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeColors.GridBg,
                ForeColor = ThemeColors.GridText,
                SelectionBackColor = ThemeColors.GridSelBg,
                SelectionForeColor = Color.White
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeColors.GridAltBg,
                ForeColor = ThemeColors.GridText,
                SelectionBackColor = ThemeColors.GridSelBg,
                SelectionForeColor = Color.White
            }
        };
        // 详情面板（右侧常显示）
        var detailPanel = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.GridBg, Padding = new Padding(10) };
        var txtDetail = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BackColor = ThemeColors.GridBg,
            ForeColor = ThemeColors.TextSecondary,
            Font = new Font("Microsoft YaHei", 10),
            BorderStyle = BorderStyle.None,
            Text = "← 选中一条记录查看详情"
        };
        detailPanel.Controls.Add(txtDetail);

        // 选中行时更新详情
        dgvRecords.SelectionChanged += (s, e) =>
        {
            if (dgvRecords.CurrentRow?.DataBoundItem == null) return;
            dynamic row = dgvRecords.CurrentRow.DataBoundItem;
            var tm = _ctx.Db.GetTestMaster((string)row.ProductId, (string)row.TestId);
            if (tm != null)
            {
                string verdict = (tm.DeltaTf <= 50 && tm.LostWeightPer <= 50 && tm.FlameDuration < 5)
                    ? "通过 — 材料判定为不燃" : "不通过";
                txtDetail.Text =
                    $"样品编号: {tm.ProductId}\r\n" +
                    $"试验标识: {tm.TestId}\r\n" +
                    $"试验日期: {tm.TestDate}\r\n" +
                    $"操作员: {tm.Operator}\r\n" +
                    $"━━━━━━━━━━━━━━━━\r\n" +
                    $"环境温度: {tm.EnvTemp:F1} °C\r\n" +
                    $"环境湿度: {tm.EnvHumidity:F1} %\r\n" +
                    $"━━━━━━━━━━━━━━━━\r\n" +
                    $"试验前质量: {tm.PreWeight:F2} g\r\n" +
                    $"试验后质量: {tm.PostWeight:F2} g\r\n" +
                    $"失重量: {tm.LostWeight:F2} g\r\n" +
                    $"失重率: {tm.LostWeightPer:F2} %\r\n" +
                    $"━━━━━━━━━━━━━━━━\r\n" +
                    $"炉温1 温升: {tm.DeltaTf1:F1} °C\r\n" +
                    $"炉温2 温升: {tm.DeltaTf2:F1} °C\r\n" +
                    $"表面温升: {tm.DeltaTs:F1} °C\r\n" +
                    $"中心温升: {tm.DeltaTc:F1} °C\r\n" +
                    $"综合温升: {tm.DeltaTf:F1} °C\r\n" +
                    $"━━━━━━━━━━━━━━━━\r\n" +
                    $"火焰持续: {tm.FlameDuration} 秒\r\n" +
                    $"试验时长: {tm.TotalTestTime} 秒\r\n" +
                    $"备注: {tm.Remark}\r\n" +
                    $"━━━━━━━━━━━━━━━━\r\n" +
                    $"判定结论: {verdict}";
            }
        };

        var splitQuery = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 550
        };
        splitQuery.Panel1.Controls.Add(dgvRecords);
        splitQuery.Panel2.Controls.Add(detailPanel);

        tabQuery.Controls.Add(splitQuery);
        tabQuery.Controls.Add(topPanel);

        var ops = _ctx.Db.GetAllOperators();
        cmbQueryOperator.Items.Add("全部");
        foreach (var op in ops) cmbQueryOperator.Items.Add(op.Username);
        cmbQueryOperator.SelectedIndex = 0;
    }

    private void RunQuery()
    {
        string? pid = string.IsNullOrWhiteSpace(txtQueryProduct.Text) ? null : txtQueryProduct.Text;
        string? op = cmbQueryOperator.SelectedIndex <= 0 ? null : cmbQueryOperator.SelectedItem?.ToString();
        string? sd = dtpStart.Value.ToString("yyyy-MM-dd");
        string? ed = dtpEnd.Value.AddDays(1).ToString("yyyy-MM-dd"); // +1天包含当天所有记录
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
            // Header
            for (int i = 0; i < dgvRecords.Columns.Count; i++)
                sheet.Cells[1, i + 1].Value = dgvRecords.Columns[i].HeaderText;
            // Data
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
        var btnCal = new Button { Text = "记录校准", Location = new Point(520, 15), Size = new Size(100, 32), BackColor = ThemeColors.AccentBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
        // WinForms Timer 在 UI 线程触发，无需 Invoke
        var temps = e.Temperatures;
        lblTF1Val.Text = $"{temps["TF1"]:F1} °C";
        lblTF2Val.Text = $"{temps["TF2"]:F1} °C";
        lblTSVal.Text = $"{temps["TS"]:F1} °C";
        lblTCVal.Text = $"{temps["TC"]:F1} °C";
        lblTCalVal.Text = $"{temps["TCal"]:F1} °C";
        lblCalTemp.Text = $"当前校准温: {temps["TCal"]:F1} °C";
        // 计时器显示
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

        // Update chart
        double t = _tc.State == TestState.Recording ? e.ElapsedSeconds : seriesTF1.Points.Count + 1;
        seriesTF1.Points.Add(new DataPoint(t, temps["TF1"]));
        seriesTF2.Points.Add(new DataPoint(t, temps["TF2"]));
        seriesTS.Points.Add(new DataPoint(t, temps["TS"]));
        seriesTC.Points.Add(new DataPoint(t, temps["TC"]));

        // Scroll X
        if (t > 600)
        {
            plotModel.Axes[0].Minimum = t - 600;
            plotModel.Axes[0].Maximum = t;
        }

        // Limit points
        foreach (var s in new[] { seriesTF1, seriesTF2, seriesTS, seriesTC })
            while (s.Points.Count > 800) s.Points.RemoveAt(0);

        plotModel.InvalidatePlot(true);

        // Log messages
        foreach (var msg in e.Messages)
        {
            Color color = msg.Message.Contains("终止") ? Color.Yellow : msg.Message.Contains("错误") ? Color.Red : Color.White;
            rtbLog.SelectionColor = color;
            rtbLog.AppendText($"{msg.Time}  {msg.Message}\n");
            rtbLog.ScrollToCaret();
        }

        // Let controller process
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
            // 清空曲线，新试验从零开始
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
            try
            {
                _ctx.ExportService.ExportCsv(tm, tempData);
                _ctx.ExportService.ExportExcel(tm, tempData);
                if (bool.TryParse(_ctx.Configuration["Report:EnablePdfExport"], out bool enablePdf) && enablePdf)
                    _ctx.ExportService.ExportPdf(tm);
                MessageBox.Show("试验记录已保存，报告已生成。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出报告失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
