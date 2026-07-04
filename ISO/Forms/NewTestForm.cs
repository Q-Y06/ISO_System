using ISO11820.Helpers;
using ISO11820.Models;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.Forms;

public partial class NewTestForm : Form
{
    public TestMaster? TestMaster { get; private set; }
    public ProductMaster? ProductMaster { get; private set; }

    private TextBox txtProductId = null!, txtTestId = null!, txtProductName = null!;
    private TextBox txtSpecification = null!, txtHeight = null!, txtDiameter = null!;
    private TextBox txtEnvTemp = null!, txtEnvHumidity = null!, txtPreWeight = null!;
    private ComboBox cmbDurationMode = null!;
    private NumericUpDown nudTargetDuration = null!;
    private Label lblOperator = null!, lblApparatus = null!;
    private Button btnOK = null!, btnCancel = null!;

    public NewTestForm()
    {
        InitializeComponent();
        LoadApparatusInfo();
    }

    private void InitializeComponent()
    {
        this.Text = "新建试验";
        this.Size = new Size(520, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ThemeColors.BgDark;
        this.ForeColor = ThemeColors.TextPrimary;

        int y = 15;
        int leftX = 130;

        AddLabel("样品编号:", 15, y); txtProductId = AddTextBox(leftX, y, 320); y += 32;
        AddLabel("试验标识:", 15, y); txtTestId = AddTextBox(leftX, y, 320); y += 32;
        AddLabel("样品名称:", 15, y); txtProductName = AddTextBox(leftX, y, 320); y += 32;
        AddLabel("规格:", 15, y); txtSpecification = AddTextBox(leftX, y, 320); y += 32;
        AddLabel("高度 (mm):", 15, y); txtHeight = AddTextBox(leftX, y, 100); y += 32;
        AddLabel("直径 (mm):", 15, y); txtDiameter = AddTextBox(leftX, y, 100); y += 32;
        AddLabel("环境温度 (°C):", 15, y); txtEnvTemp = AddTextBox(leftX, y, 100); txtEnvTemp.Text = "25.0"; y += 32;
        AddLabel("环境湿度 (%):", 15, y); txtEnvHumidity = AddTextBox(leftX, y, 100); txtEnvHumidity.Text = "50.0"; y += 32;
        AddLabel("试验前质量 (g):", 15, y); txtPreWeight = AddTextBox(leftX, y, 100); txtPreWeight.Text = "50.0"; y += 32;

        AddLabel("时长模式:", 15, y);
        cmbDurationMode = new ComboBox
        {
            Location = new Point(leftX, y - 2), Size = new Size(120, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary,
            FlatStyle = FlatStyle.Flat
        };
        cmbDurationMode.Items.AddRange(new[] { "标准 60 分钟", "自定义" });
        cmbDurationMode.SelectedIndex = 0;
        this.Controls.Add(cmbDurationMode);
        y += 32;

        AddLabel("自定义时长 (秒):", 15, y);
        nudTargetDuration = new NumericUpDown
        {
            Location = new Point(leftX, y - 2), Size = new Size(100, 24),
            Minimum = 60, Maximum = 7200, Value = 3600, Enabled = false,
            BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary
        };
        this.Controls.Add(nudTargetDuration);
        cmbDurationMode.SelectedIndexChanged += (s, e) => nudTargetDuration.Enabled = cmbDurationMode.SelectedIndex == 1;
        y += 32;

        AddLabel("操作员:", 15, y);
        lblOperator = new Label
        {
            Text = AppContext.Instance.CurrentOperator,
            Location = new Point(leftX, y), Size = new Size(200, 22),
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            ForeColor = ThemeColors.TextPrimary
        };
        this.Controls.Add(lblOperator);
        y += 32;

        AddLabel("设备信息:", 15, y);
        lblApparatus = new Label
        {
            Text = "--", Location = new Point(leftX, y), Size = new Size(320, 22),
            Font = new Font("Microsoft YaHei", 9),
            ForeColor = ThemeColors.TextSecondary
        };
        this.Controls.Add(lblApparatus);
        y += 42;

        btnOK = new Button
        {
            Text = "创建试验", Location = new Point(120, y), Size = new Size(110, 38),
            BackColor = ThemeColors.LoginBlue, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOK.FlatAppearance.BorderSize = 0;
        btnOK.Click += BtnOK_Click;
        btnCancel = new Button
        {
            Text = "取消", Location = new Point(250, y), Size = new Size(100, 38),
            BackColor = ThemeColors.LoginGray, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.Add(btnOK);
        this.Controls.Add(btnCancel);
    }

    private void AddLabel(string text, int x, int y)
    {
        this.Controls.Add(new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(110, 22),
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Microsoft YaHei", 9),
            ForeColor = ThemeColors.TextPrimary
        });
    }

    private TextBox AddTextBox(int x, int y, int width)
    {
        var tb = new TextBox
        {
            Location = new Point(x, y), Size = new Size(width, 24),
            Font = new Font("Microsoft YaHei", 9),
            BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(tb);
        return tb;
    }

    private void LoadApparatusInfo()
    {
        var app = AppContext.Instance.Db.GetApparatus("ISO11820-001");
        if (app != null)
            lblApparatus.Text = $"{app.ApparatusName} | 编号:{app.ApparatusId} | 检定:{app.CalibrationDate} | 恒功率:{app.ConstPower}W";
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtProductId.Text) || string.IsNullOrWhiteSpace(txtTestId.Text))
        {
            MessageBox.Show("样品编号和试验标识为必填项", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var app = AppContext.Instance.Db.GetApparatus("ISO11820-001");

        TestMaster = new TestMaster
        {
            ProductId = txtProductId.Text.Trim(),
            TestId = txtTestId.Text.Trim(),
            TestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Operator = AppContext.Instance.CurrentOperator,
            EnvTemp = double.TryParse(txtEnvTemp.Text, out double et) ? et : 25.0,
            EnvHumidity = double.TryParse(txtEnvHumidity.Text, out double eh) ? eh : 50.0,
            PreWeight = double.TryParse(txtPreWeight.Text, out double pw) ? pw : 50.0,
            DurationMode = cmbDurationMode.SelectedIndex == 0 ? "Standard" : "Fixed",
            TargetDuration = (int)nudTargetDuration.Value,
            ApparatusId = app?.ApparatusId ?? "ISO11820-001",
            ApparatusName = app?.ApparatusName ?? "不燃性试验炉",
            ApparatusCalibrationDate = app?.CalibrationDate ?? "",
            ConstPower = app?.ConstPower ?? 2048,
            Flag = ""
        };

        ProductMaster = new ProductMaster
        {
            ProductId = txtProductId.Text.Trim(),
            TestId = txtTestId.Text.Trim(),
            ProductName = txtProductName.Text.Trim(),
            Specification = txtSpecification.Text.Trim(),
            Height = double.TryParse(txtHeight.Text, out double h) ? h : 0,
            Diameter = double.TryParse(txtDiameter.Text, out double d) ? d : 0
        };

        this.DialogResult = DialogResult.OK;
    }
}
