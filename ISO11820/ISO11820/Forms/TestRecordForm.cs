using ISO11820.Helpers;
using ISO11820.Models;

namespace ISO11820.Forms;

public partial class TestRecordForm : Form
{
    public double PostWeight { get; private set; }
    public bool HasFlame { get; private set; }
    public int FlameStartTime { get; private set; }
    public int FlameDuration { get; private set; }
    public string Remark { get; private set; } = string.Empty;

    private CheckBox chkFlame = null!;
    private NumericUpDown nudFlameStart = null!, nudFlameDuration = null!;
    private TextBox txtPostWeight = null!, txtRemark = null!;
    private Button btnOK = null!, btnCancel = null!;

    public TestRecordForm(TestMaster tm)
    {
        InitializeComponent();
        this.Text = $"试验记录 — {tm.ProductId} / {tm.TestId}";

        var lblPreWeight = new Label
        {
            Text = $"试验前质量: {tm.PreWeight:F2} g",
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            ForeColor = ThemeColors.TextPrimary,
            Location = new Point(15, 15),
            Size = new Size(250, 24)
        };
        this.Controls.Add(lblPreWeight);
    }

    private void InitializeComponent()
    {
        this.Size = new Size(440, 380);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ThemeColors.BgDark;
        this.ForeColor = ThemeColors.TextPrimary;

        int y = 50;
        int leftX = 150;

        var lblPostWeight = new Label { Text = "试验后质量 (g):", Location = new Point(15, y), Size = new Size(130, 24), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Microsoft YaHei", 9), ForeColor = ThemeColors.TextPrimary };
        txtPostWeight = new TextBox { Location = new Point(leftX, y), Size = new Size(100, 24), Font = new Font("Microsoft YaHei", 9), BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        this.Controls.Add(lblPostWeight);
        this.Controls.Add(txtPostWeight);
        y += 38;

        chkFlame = new CheckBox { Text = "是否出现持续火焰", Location = new Point(leftX, y), Size = new Size(160, 24), Font = new Font("Microsoft YaHei", 9), Checked = false, ForeColor = ThemeColors.TextPrimary };
        chkFlame.CheckedChanged += (s, e) => { nudFlameStart.Enabled = nudFlameDuration.Enabled = chkFlame.Checked; };
        this.Controls.Add(chkFlame);
        y += 32;

        var lblFlameStart = new Label { Text = "火焰发生时刻 (秒):", Location = new Point(15, y), Size = new Size(130, 24), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Microsoft YaHei", 9), ForeColor = ThemeColors.TextPrimary };
        nudFlameStart = new NumericUpDown { Location = new Point(leftX, y), Size = new Size(100, 24), Minimum = 0, Maximum = 7200, Enabled = false, BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary };
        this.Controls.Add(lblFlameStart);
        this.Controls.Add(nudFlameStart);
        y += 32;

        var lblFlameDuration = new Label { Text = "火焰持续时间 (秒):", Location = new Point(15, y), Size = new Size(130, 24), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Microsoft YaHei", 9), ForeColor = ThemeColors.TextPrimary };
        nudFlameDuration = new NumericUpDown { Location = new Point(leftX, y), Size = new Size(100, 24), Minimum = 0, Maximum = 7200, Enabled = false, BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary };
        this.Controls.Add(lblFlameDuration);
        this.Controls.Add(nudFlameDuration);
        y += 38;

        var lblRemark = new Label { Text = "备注:", Location = new Point(15, y), Size = new Size(130, 24), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Microsoft YaHei", 9), ForeColor = ThemeColors.TextPrimary };
        txtRemark = new TextBox { Location = new Point(leftX, y), Size = new Size(250, 60), Multiline = true, Font = new Font("Microsoft YaHei", 9), BackColor = ThemeColors.BgInput, ForeColor = ThemeColors.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        this.Controls.Add(lblRemark);
        this.Controls.Add(txtRemark);
        y += 75;

        btnOK = new Button { Text = "保存", Location = new Point(110, y), Size = new Size(100, 38), BackColor = ThemeColors.AccentGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnOK.FlatAppearance.BorderSize = 0;
        btnOK.Click += BtnOK_Click;
        btnCancel = new Button { Text = "取消", Location = new Point(230, y), Size = new Size(100, 38), BackColor = ThemeColors.LoginGray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.Add(btnOK);
        this.Controls.Add(btnCancel);
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPostWeight.Text) || !double.TryParse(txtPostWeight.Text, out double pw))
        {
            MessageBox.Show("请输入有效的试验后质量", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PostWeight = pw;
        HasFlame = chkFlame.Checked;
        FlameStartTime = (int)nudFlameStart.Value;
        FlameDuration = (int)nudFlameDuration.Value;
        Remark = txtRemark.Text;

        this.DialogResult = DialogResult.OK;
    }
}
