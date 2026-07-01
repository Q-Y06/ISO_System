using ISO11820.Global;
using ISO11820.Models;
using Serilog;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.UI.Forms;

public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        this.Text = "ISO 11820 试验系统";
        this.Size = new Size(400, 380);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = Color.White;

        // 标题区
        var titlePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(30, 100, 200)
        };

        var lblTitle = new Label
        {
            Text = "ISO 11820",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        titlePanel.Controls.Add(lblTitle);

        var lblSubtitle = new Label
        {
            Text = "建筑材料不燃性试验仿真系统",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(200, 220, 255),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Bottom,
            Height = 25
        };
        titlePanel.Controls.Add(lblSubtitle);

        this.Controls.Add(titlePanel);

        // 角色选择
        var lblRole = new Label
        {
            Text = "选择身份",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(50, 105),
            Size = new Size(100, 25),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        this.Controls.Add(lblRole);

        var cmbRole = new ComboBox
        {
            Location = new Point(155, 103),
            Size = new Size(180, 28),
            Font = new Font("Segoe UI", 11),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
        cmbRole.Items.AddRange(new[] { "管理员 (admin)", "试验员 (experimenter)" });
        cmbRole.SelectedIndex = 0;
        this.Controls.Add(cmbRole);

        // 密码
        var lblPwd = new Label
        {
            Text = "访问密码",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(50, 150),
            Size = new Size(100, 25),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        this.Controls.Add(lblPwd);

        var txtPassword = new TextBox
        {
            Location = new Point(155, 148),
            Size = new Size(180, 30),
            PasswordChar = '●',
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "默认密码 123456"
        };
        txtPassword.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) DoLogin(cmbRole.SelectedIndex == 0, txtPassword.Text.Trim());
        };
        this.Controls.Add(txtPassword);

        // 错误提示
        var lblError = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(220, 50, 50),
            Location = new Point(50, 190),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(lblError);

        // 登录按钮
        var btnLogin = new Button
        {
            Text = "登  录",
            Location = new Point(100, 230),
            Size = new Size(200, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(30, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += (s, e) =>
        {
            var isAdmin = cmbRole.SelectedIndex == 0;
            var pwd = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(pwd))
            {
                lblError.Text = "请输入密码";
                return;
            }

            var username = isAdmin ? "admin" : "experimenter";
            var ctx = AppContext.Instance;
            if (ctx.Db.ValidateLogin(username, pwd, out var userId, out var userType))
            {
                ctx.CurrentUser = new Operator { UserId = userId, UserName = username, UserType = userType };
                Log.Information("登录成功: {User}", username);
                ctx.StartDaq();
                var main = new MainForm();
                this.Hide();
                main.FormClosed += (_, _) => this.Close();
                main.Show();
            }
            else
            {
                lblError.Text = "密码错误，请重新输入";
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        };
        this.Controls.Add(btnLogin);

        // 底部提示
        var lblHint = new Label
        {
            Text = "默认密码: 123456  |  管理员: admin  |  试验员: experimenter",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            Location = new Point(50, 290),
            Size = new Size(300, 25),
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(lblHint);
    }

    /// <summary>
    /// 保留旧接口兼容性
    /// </summary>
    private void DoLogin(bool isAdmin, string pwd) { }
}
