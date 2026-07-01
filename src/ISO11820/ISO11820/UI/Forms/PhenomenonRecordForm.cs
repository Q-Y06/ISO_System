using ISO11820.Global;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.UI.Forms;

/// <summary>
/// 试验现象记录窗口 — 试验完成后填写
/// </summary>
public partial class PhenomenonRecordForm : Form
{
    private readonly AppContext _ctx;
    private CheckBox _chkFlame = null!;
    private NumericUpDown _nudFlameTime = null!;
    private NumericUpDown _nudFlameDuration = null!;
    private NumericUpDown _nudPostWeight = null!;
    private TextBox _txtMemo = null!;

    public PhenomenonRecordForm()
    {
        _ctx = AppContext.Instance;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "试验记录";
        this.Size = new Size(450, 420);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(45, 45, 45);
        this.ForeColor = Color.White;

        int y = 15;

        var test = _ctx.TestController.CurrentTest;
        if (test != null)
        {
            var lblTest = new Label
            {
                Text = $"试验: {test.ProductId} / {test.TestId}",
                Location = new Point(15, y),
                Size = new Size(400, 25),
                Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = Color.Cyan
            };
            this.Controls.Add(lblTest);
            y += 30;

            // 试验前质量（显示）
            var lblPreWeight = new Label
            {
                Text = $"试验前质量: {test.PreWeight:F2} g",
                Location = new Point(15, y),
                Size = new Size(250, 25),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblPreWeight);
        }
        y += 30;

        // 试验后质量（必填）
        AddLabel("试验后质量 (g) *:", 15, y);
        _nudPostWeight = new NumericUpDown
        {
            Location = new Point(150, y),
            Size = new Size(120, 25),
            Minimum = 0,
            Maximum = 10000,
            Value = 140,
            DecimalPlaces = 2
        };
        this.Controls.Add(_nudPostWeight);
        y += 35;

        // 火焰复选框
        _chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(15, y),
            Size = new Size(200, 25),
            BackColor = Color.Transparent,
            Checked = false
        };
        _chkFlame.CheckedChanged += (s, e) =>
        {
            _nudFlameTime.Enabled = _chkFlame.Checked;
            _nudFlameDuration.Enabled = _chkFlame.Checked;
        };
        this.Controls.Add(_chkFlame);
        y += 30;

        // 火焰发生时刻
        AddLabel("火焰发生时刻 (秒):", 35, y);
        _nudFlameTime = new NumericUpDown
        {
            Location = new Point(170, y),
            Size = new Size(100, 25),
            Minimum = 0,
            Maximum = 3600,
            Enabled = false
        };
        this.Controls.Add(_nudFlameTime);
        y += 30;

        // 火焰持续时间
        AddLabel("火焰持续时间 (秒):", 35, y);
        _nudFlameDuration = new NumericUpDown
        {
            Location = new Point(170, y),
            Size = new Size(100, 25),
            Minimum = 0,
            Maximum = 3600,
            Enabled = false
        };
        this.Controls.Add(_nudFlameDuration);
        y += 35;

        // 备注
        AddLabel("备注:", 15, y);
        _txtMemo = new TextBox
        {
            Location = new Point(70, y),
            Size = new Size(340, 60),
            Multiline = true,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        this.Controls.Add(_txtMemo);
        y += 70;

        // 保存按钮
        var btnSave = new Button
        {
            Text = "保存试验记录",
            Location = new Point(130, y + 10),
            Size = new Size(180, 36),
            BackColor = Color.FromArgb(0, 150, 0),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold)
        };
        btnSave.Click += (s, e) => SaveRecord();
        this.Controls.Add(btnSave);
    }

    private void AddLabel(string text, int x, int y)
    {
        this.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y + 3),
            Size = new Size(140, 25),
            BackColor = Color.Transparent
        });
    }

    private void SaveRecord()
    {
        if (_nudPostWeight.Value <= 0)
        {
            MessageBox.Show("请输入试验后质量", "提示");
            _nudPostWeight.Focus();
            return;
        }

        bool success = _ctx.TestController.SaveTestResult(
            postWeight: (double)_nudPostWeight.Value,
            hasFlame: _chkFlame.Checked,
            flameTime: (int)_nudFlameTime.Value,
            flameDuration: (int)_nudFlameDuration.Value,
            memo: string.IsNullOrWhiteSpace(_txtMemo.Text) ? null : _txtMemo.Text.Trim()
        );

        if (success)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            MessageBox.Show("保存失败，请重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
