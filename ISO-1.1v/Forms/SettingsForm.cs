using ISO11820.Global;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.Forms;

public partial class SettingsForm : Form
{
    private NumericUpDown nudInitTemp = null!, nudTargetTemp = null!;
    private NumericUpDown nudHeatRate = null!, nudFluctuation = null!;
    private Button btnOK = null!, btnCancel = null!;

    public SettingsForm()
    {
        InitializeComponent();
        LoadValues();
    }

    private void InitializeComponent()
    {
        this.Text = "仿真参数设置";
        this.Size = new Size(380, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 20;
        var cfg = AppContext.Instance.SimulationConfig;

        nudInitTemp = AddRow("初始炉温 (°C):", y, 0, 1000); y += 35;
        nudTargetTemp = AddRow("目标炉温 (°C):", y, 0, 1000); y += 35;
        nudHeatRate = AddRow("升温速率 (°C/s):", y, 1, 100); y += 35;
        nudFluctuation = AddRow("温度波动 (±°C):", y, 0, 10); y += 50;

        btnOK = new Button { Text = "保存", Location = new Point(100, y), Size = new Size(80, 32),
            BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnOK.Click += (s, e) => { SaveValues(); this.DialogResult = DialogResult.OK; };
        btnCancel = new Button { Text = "取消", Location = new Point(200, y), Size = new Size(80, 32), FlatStyle = FlatStyle.Flat };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.Add(btnOK);
        this.Controls.Add(btnCancel);
    }

    private NumericUpDown AddRow(string label, int y, decimal min, decimal max)
    {
        this.Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), Size = new Size(150, 22), Font = new Font("Microsoft YaHei", 9) });
        var nud = new NumericUpDown { Location = new Point(175, y), Size = new Size(80, 24), Minimum = min, Maximum = max, DecimalPlaces = 1, Font = new Font("Microsoft YaHei", 9) };
        this.Controls.Add(nud);
        return nud;
    }

    private void LoadValues()
    {
        var cfg = AppContext.Instance.SimulationConfig;
        nudInitTemp.Value = (decimal)cfg.InitialFurnaceTemp;
        nudTargetTemp.Value = (decimal)cfg.TargetFurnaceTemp;
        nudHeatRate.Value = (decimal)cfg.HeatingRatePerSecond;
        nudFluctuation.Value = (decimal)cfg.TempFluctuation;
    }

    private void SaveValues()
    {
        var cfg = AppContext.Instance.SimulationConfig;
        cfg.InitialFurnaceTemp = (double)nudInitTemp.Value;
        cfg.TargetFurnaceTemp = (double)nudTargetTemp.Value;
        cfg.HeatingRatePerSecond = (double)nudHeatRate.Value;
        cfg.TempFluctuation = (double)nudFluctuation.Value;
    }
}
