using ISO11820.Helpers;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.Forms;

public partial class LoginForm : Form
{
    private RadioButton rbAdmin = null!, rbExperimenter = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!, btnCancel = null!;
    private Label lblTitle = null!, lblPassword = null!;
    private GroupBox gbRole = null!;

    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "ISO 11820 不燃性试验系统 — 登录";
        this.Size = new Size(420, 320);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = ThemeColors.BgDark;
        this.ForeColor = ThemeColors.TextPrimary;

        lblTitle = new Label
        {
            Text = "ISO 11820 建筑材料不燃性试验系统",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            ForeColor = ThemeColors.TextPrimary,
            Size = new Size(380, 35),
            Location = new Point(20, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        gbRole = new GroupBox
        {
            Text = "选择角色",
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 10),
            Size = new Size(360, 65),
            Location = new Point(20, 65)
        };

        rbAdmin = new RadioButton
        {
            Text = "管理员",
            Location = new Point(30, 28),
            Size = new Size(80, 22),
            Checked = true,
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 10)
        };

        rbExperimenter = new RadioButton
        {
            Text = "试验员",
            Location = new Point(130, 28),
            Size = new Size(80, 22),
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 10)
        };

        gbRole.Controls.Add(rbAdmin);
        gbRole.Controls.Add(rbExperimenter);

        lblPassword = new Label
        {
            Text = "输入密码:",
            ForeColor = ThemeColors.TextPrimary,
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(20, 145),
            Size = new Size(80, 25)
        };

        txtPassword = new TextBox
        {
            Location = new Point(110, 143),
            Size = new Size(200, 28),
            PasswordChar = '*',
            BackColor = ThemeColors.BgInput,
            ForeColor = ThemeColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei", 10)
        };

        btnLogin = new Button
        {
            Text = "登 录",
            Location = new Point(80, 200),
            Size = new Size(100, 38),
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            BackColor = ThemeColors.LoginBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        btnCancel = new Button
        {
            Text = "退 出",
            Location = new Point(220, 200),
            Size = new Size(100, 38),
            Font = new Font("Microsoft YaHei", 10),
            BackColor = ThemeColors.LoginGray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => Application.Exit();

        this.Controls.Add(lblTitle);
        this.Controls.Add(gbRole);
        this.Controls.Add(lblPassword);
        this.Controls.Add(txtPassword);
        this.Controls.Add(btnLogin);
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnLogin;
        txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e); };
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = rbAdmin.Checked ? "admin" : "experimenter";
        string password = txtPassword.Text;

        var op = AppContext.Instance.Db.ValidateLogin(username, password);
        if (op != null)
        {
            AppContext.Instance.CurrentOperator = op.Username;
            AppContext.Instance.CurrentRole = op.Role;

            this.Hide();
            var mainForm = new MainForm();
            mainForm.FormClosed += (s, args) => this.Close();
            mainForm.Show();
        }
        else
        {
            MessageBox.Show("密码错误，请重新输入", "登录失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.SelectAll();
            txtPassword.Focus();
        }
    }
}
