using ISO11820.Core;

#nullable disable warnings

namespace ISO11820.Forms;

/// <summary>
/// 登录窗体 — 现代风格
/// </summary>
public partial class LoginForm : Form
{
#nullable disable
    private RadioButton rbAdmin;
    private RadioButton rbExperimenter;
    private TextBox txtPassword;
    private Button btnLogin;
    private Label lblError;

    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.AutoScaleMode = AutoScaleMode.Font;
        this.Text = "";
        this.Size = new Size(520, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 40);

        // 圆角边框 — 用Panel代替
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 40),
            Padding = new Padding(1)
        };

        // 标题栏（可拖动）
        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.FromArgb(22, 22, 32)
        };

        var lblTitleBar = new Label
        {
            Text = "  ISO 11820 不燃性试验系统",
            ForeColor = Color.FromArgb(180, 180, 200),
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(12, 10),
            AutoSize = true
        };

        var btnClose = new Label
        {
            Text = "✕",
            ForeColor = Color.FromArgb(180, 180, 200),
            Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
            Location = new Point(480, 7),
            Size = new Size(28, 28),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => Application.Exit();
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(180, 180, 200);

        titleBar.Controls.Add(lblTitleBar);
        titleBar.Controls.Add(btnClose);
        // 拖动
        Point dragOffset = Point.Empty;
        titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragOffset = new Point(e.X, e.Y); };
        titleBar.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) this.Location = new Point(this.Left + e.X - dragOffset.X, this.Top + e.Y - dragOffset.Y); };

        // 白色卡片区域
        var cardPanel = new Panel
        {
            Location = new Point(40, 55),
            Size = new Size(440, 420),
            BackColor = Color.White
        };

        // Logo/图标区域
        var iconLabel = new Label
        {
            Text = "🔥",
            Font = new Font("Segoe UI Emoji", 40),
            Location = new Point(0, 12),
            Size = new Size(440, 64),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        // 主标题
        var lblTitle = new Label
        {
            Text = "ISO 11820 不燃性试验系统",
            Font = new Font("Microsoft YaHei", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 30, 40),
            Location = new Point(0, 76),
            Size = new Size(440, 34),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var lblSubtitle = new Label
        {
            Text = "建筑材料不燃性试验仿真平台",
            Font = new Font("Microsoft YaHei", 9),
            ForeColor = Color.FromArgb(140, 140, 150),
            Location = new Point(0, 114),
            Size = new Size(440, 22),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        // 分隔线
        var divider = new Panel
        {
            Location = new Point(60, 154),
            Size = new Size(320, 1),
            BackColor = Color.FromArgb(220, 220, 225)
        };

        // 角色选择区域
        var roleLabel = new Label
        {
            Text = "选择角色",
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 60, 70),
            Location = new Point(65, 172),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        rbAdmin = new RadioButton
        {
            Text = "管理员",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(65, 202),
            Size = new Size(130, 30),
            Checked = true,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat
        };

        rbExperimenter = new RadioButton
        {
            Text = "试验员",
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(220, 202),
            Size = new Size(130, 30),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat
        };

        // 密码输入
        var pwdLabel = new Label
        {
            Text = "输入密码",
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 60, 70),
            Location = new Point(65, 248),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        txtPassword = new TextBox
        {
            Location = new Point(65, 278),
            Size = new Size(310, 40),
            Font = new Font("Microsoft YaHei", 12),
            PasswordChar = '●',
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(245, 245, 248)
        };
        txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnLogin.PerformClick(); };

        // 错误提示
        lblError = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(220, 50, 50),
            Location = new Point(65, 324),
            AutoSize = true,
            Font = new Font("Microsoft YaHei", 9),
            BackColor = Color.Transparent
        };

        // 登录按钮
        btnLogin = new Button
        {
            Text = "登  录",
            Location = new Point(65, 350),
            Size = new Size(310, 44),
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(45, 125, 210),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;
        // 悬停效果
        btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(35, 105, 185);
        btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Color.FromArgb(45, 125, 210);

        cardPanel.Controls.AddRange(new Control[] {
            iconLabel, lblTitle, lblSubtitle, divider,
            roleLabel, rbAdmin, rbExperimenter,
            pwdLabel, txtPassword, lblError, btnLogin
        });

        mainPanel.Controls.Add(titleBar);
        mainPanel.Controls.Add(cardPanel);
        this.Controls.Add(mainPanel);
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = rbAdmin.Checked ? "admin" : "experimenter";
        string pwd = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(pwd))
        {
            lblError.Text = "请输入密码";
            return;
        }

        var db = AppGlobal.Instance.Db;
        if (db.Login(username, pwd, out string userid, out string usertype))
        {
            AppGlobal.Instance.CurrentUserId = userid;
            AppGlobal.Instance.CurrentUsername = username;
            AppGlobal.Instance.CurrentUserType = usertype;
            lblError.Text = "";

            this.Hide();
            var mainForm = new MainForm();
            mainForm.FormClosed += (s, args) => this.Close();
            mainForm.Show();
        }
        else
        {
            lblError.Text = "密码错误，请重新输入";
            txtPassword.SelectAll();
            txtPassword.Focus();
        }
    }
}