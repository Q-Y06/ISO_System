using ISO11820.Core;
using ISO11820.Global;
using ISO11820.Models;
using Serilog;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.UI.Forms;

/// <summary>
/// 新建试验窗口
/// </summary>
public partial class NewTestForm : Form
{
    private readonly AppContext _ctx;

    private TextBox _txtProductId = null!;
    private TextBox _txtTestId = null!;
    private TextBox _txtProductName = null!;
    private TextBox _txtSpecific = null!;
    private NumericUpDown _nudDiameter = null!;
    private NumericUpDown _nudHeight = null!;
    private NumericUpDown _nudAmbTemp = null!;
    private NumericUpDown _nudAmbHumi = null!;
    private NumericUpDown _nudPreWeight = null!;
    private NumericUpDown _nudCustomDuration = null!;
    private RadioButton _rbStandard = null!;
    private RadioButton _rbCustom = null!;
    private Label _lblApparatusInfo = null!;

    public NewTestForm()
    {
        _ctx = AppContext.Instance;
        InitializeComponent();
        LoadApparatusInfo();
    }

    private void InitializeComponent()
    {
        this.Text = "新建试验";
        this.Size = new Size(500, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(45, 45, 45);
        this.ForeColor = Color.White;

        int y = 15;

        // 试验ID（自动生成）
        AddLabel("试验ID:", 10, y);
        _txtTestId = AddTextBox(110, y, 150);
        _txtTestId.Text = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        _txtTestId.ReadOnly = true;
        _txtTestId.BackColor = Color.FromArgb(60, 60, 60);
        y += 32;

        // 样品编号
        AddLabel("样品编号:", 10, y);
        _txtProductId = AddTextBox(110, y, 150);
        _txtProductId.Text = DateTime.Now.ToString("yyyyMMdd") + "-001";
        y += 32;

        // 样品名称
        AddLabel("样品名称:", 10, y);
        _txtProductName = AddTextBox(110, y, 180);
        _txtProductName.Text = "岩棉隔热板";
        y += 32;

        // 规格型号
        AddLabel("规格型号:", 10, y);
        _txtSpecific = AddTextBox(110, y, 180);
        _txtSpecific.Text = "100×50×25mm";
        y += 32;

        // 直径/高度
        AddLabel("直径 (mm):", 10, y);
        _nudDiameter = new NumericUpDown { Location = new Point(110, y), Size = new Size(80, 25),
            Minimum = 1, Maximum = 1000, Value = 100, DecimalPlaces = 1 };
        this.Controls.Add(_nudDiameter);

        AddLabel("高度 (mm):", 210, y);
        _nudHeight = new NumericUpDown { Location = new Point(300, y), Size = new Size(80, 25),
            Minimum = 1, Maximum = 1000, Value = 50, DecimalPlaces = 1 };
        this.Controls.Add(_nudHeight);
        y += 32;

        // 环境温度/湿度
        AddLabel("环境温度 (°C):", 10, y);
        _nudAmbTemp = new NumericUpDown { Location = new Point(110, y), Size = new Size(80, 25),
            Minimum = -10, Maximum = 60, Value = 25, DecimalPlaces = 1 };
        this.Controls.Add(_nudAmbTemp);

        AddLabel("环境湿度 (%):", 210, y);
        _nudAmbHumi = new NumericUpDown { Location = new Point(300, y), Size = new Size(80, 25),
            Minimum = 0, Maximum = 100, Value = 50, DecimalPlaces = 1 };
        this.Controls.Add(_nudAmbHumi);
        y += 32;

        // 试验前质量
        AddLabel("试验前质量 (g):", 10, y);
        _nudPreWeight = new NumericUpDown { Location = new Point(110, y), Size = new Size(100, 25),
            Minimum = 0, Maximum = 10000, Value = 150, DecimalPlaces = 2 };
        this.Controls.Add(_nudPreWeight);
        y += 32;

        // 试验时长模式
        AddLabel("试验时长:", 10, y);
        _rbStandard = new RadioButton { Text = "标准 60 分钟", Location = new Point(110, y), Size = new Size(120, 25),
            BackColor = Color.Transparent, Checked = true };
        this.Controls.Add(_rbStandard);
        _rbCustom = new RadioButton { Text = "自定义 (分钟):", Location = new Point(240, y), Size = new Size(120, 25),
            BackColor = Color.Transparent };
        this.Controls.Add(_rbCustom);
        _nudCustomDuration = new NumericUpDown { Location = new Point(355, y), Size = new Size(70, 25),
            Minimum = 1, Maximum = 120, Value = 30, Enabled = false };
        _rbCustom.CheckedChanged += (s, e) => _nudCustomDuration.Enabled = _rbCustom.Checked;
        this.Controls.Add(_nudCustomDuration);
        y += 32;

        // 设备信息
        AddLabel("设备信息:", 10, y);
        _lblApparatusInfo = new Label
        {
            Location = new Point(110, y),
            Size = new Size(350, 40),
            BackColor = Color.Transparent,
            ForeColor = Color.LightGray,
            Font = new Font("Microsoft YaHei UI", 8f)
        };
        this.Controls.Add(_lblApparatusInfo);
        y += 50;

        // 创建按钮
        var btnCreate = new Button
        {
            Text = "创建试验",
            Location = new Point(150, y + 10),
            Size = new Size(180, 36),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold)
        };
        btnCreate.Click += (s, e) => CreateTest();
        this.Controls.Add(btnCreate);
    }

    private void AddLabel(string text, int x, int y)
    {
        this.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y + 3),
            Size = new Size(100, 25),
            BackColor = Color.Transparent
        });
    }

    private TextBox AddTextBox(int x, int y, int width)
    {
        var tb = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 25),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        this.Controls.Add(tb);
        return tb;
    }

    private void LoadApparatusInfo()
    {
        var apparatus = _ctx.Db.GetApparatus(0);
        if (apparatus != null)
        {
            _lblApparatusInfo.Text = $"{apparatus.ApparatusName} ({apparatus.InnerNumber})\n"
                + $"检定有效期: {apparatus.CheckDateF:yyyy-MM-dd} ~ {apparatus.CheckDateT:yyyy-MM-dd}\n"
                + $"恒功率: {apparatus.ConstPower ?? 2048}";
        }
    }

    private void CreateTest()
    {
        var productId = _txtProductId.Text.Trim();
        var testId = _txtTestId.Text.Trim();

        if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(testId))
        {
            MessageBox.Show("样品编号和试验ID不能为空", "提示");
            return;
        }

        if (_ctx.Db.ProductExists(productId))
        {
            var result = MessageBox.Show($"样品编号 {productId} 已存在，是否使用已有样品？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
                return;
        }

        var apparatus = _ctx.Db.GetApparatus(0);
        int duration = _rbStandard.Checked ? 3600 : (int)_nudCustomDuration.Value * 60;
        bool isFixed = _rbCustom.Checked;

        var product = new ProductMaster
        {
            ProductId = productId,
            ProductName = _txtProductName.Text.Trim(),
            Specific = _txtSpecific.Text.Trim(),
            Diameter = (double)_nudDiameter.Value,
            Height = (double)_nudHeight.Value
        };

        var test = new TestMaster
        {
            ProductId = productId,
            TestId = testId,
            TestDate = DateTime.Now,
            AmbTemp = (double)_nudAmbTemp.Value,
            AmbHumi = (double)_nudAmbHumi.Value,
            According = "ISO 11820:2022",
            Operator = _ctx.CurrentUser?.UserName ?? "unknown",
            ApparatusId = apparatus?.ApparatusId.ToString() ?? "0",
            ApparatusName = apparatus?.ApparatusName ?? "一号试验炉",
            ApparatusChkDate = apparatus?.CheckDateF ?? DateTime.Now,
            RptNo = productId,
            PreWeight = (double)_nudPreWeight.Value,
            ConstPower = apparatus?.ConstPower ?? 2048
        };

        if (_controller.CreateNewTest(product, test, duration, isFixed))
        {
            Log.Information("新建试验成功: {ProductId}/{TestId}", productId, testId);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            MessageBox.Show("创建试验失败：可能存在未保存的试验记录", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private TestController _controller => _ctx.TestController;
}
