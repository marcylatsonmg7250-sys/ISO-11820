using ISO11820.Core;
using ISO11820.Models;

#nullable disable warnings

namespace ISO11820.Forms;

public partial class NewTestForm : Form
{
#nullable disable
    private TextBox txtAmbTemp, txtAmbHumi;
    private TextBox txtProductId, txtProductName, txtSpecific, txtHeight, txtDiameter;
    private TextBox txtTestId, txtOperator, txtCustomDuration;
    private ComboBox cmbDurationMode;
    private Label lblCustomDuration;
    private TextBox txtPreWeight;
    private TextBox txtApparatusId, txtApparatusName, txtCheckDate, txtConstPower;
    private Button btnCreate, btnCancel;

    public bool TestCreated { get; private set; }
    public string? ProductId { get; private set; }
    public string? TestId { get; private set; }
    public string ProductNameVal { get; private set; } = "";

    public NewTestForm()
    {
        InitializeComponent();
        LoadDeviceInfo();
    }

    private void InitializeComponent()
    {
        this.Text = "新建试验";
        this.Size = new Size(820, 800);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(245, 245, 250);

        // 页面标题
        var lblTitle = new Label
        {
            Text = "新建试验",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 30, 40),
            Location = new Point(28, 22),
            AutoSize = true
        };

        var lblHint = new Label
        {
            Text = "填写样品信息与试验参数，完成后点击「创建试验」",
            Font = new Font("Microsoft YaHei", 9),
            ForeColor = Color.FromArgb(150, 150, 160),
            Location = new Point(28, 64),
            AutoSize = true
        };

        // ===== 布局定义 =====
        int leftX = 28;        // 左列起始
        int rightX = 425;      // 右列起始
        int colW = 350;        // 每列内容宽度
        int lblW = 100;        // 标签宽度
        int fieldW = colW - lblW - 10; // 输入框宽度
        int rowH = 40;         // 行高
        int startY = 96;       // 起始Y

        // ===== 左列 =====
        int y = startY;

        // -- 环境信息 --
        AddSectionTitle(this, leftX, y, colW, "🌡 环境信息"); y += 36;
        AddRow(this, leftX, y, lblW, fieldW, "环境温度 (°C)", "25.0", out txtAmbTemp); y += rowH;
        AddRow(this, leftX, y, lblW, fieldW, "环境湿度 (%)", "50.0", out txtAmbHumi); y += rowH + 10;

        // -- 样品信息 --
        AddSectionTitle(this, leftX, y, colW, "📦 样品信息"); y += 30;
        AddRow(this, leftX, y, lblW, fieldW, "样品编号", $"SP-{DateTime.Now:yyyyMMdd}-001", out txtProductId); y += rowH;
        AddRow(this, leftX, y, lblW, fieldW, "样品名称", "", out txtProductName); y += rowH;
        AddRow(this, leftX, y, lblW, fieldW, "规格型号", "", out txtSpecific); y += rowH;
        AddRow(this, leftX, y, lblW, fieldW, "高度 (mm)", "50", out txtHeight); y += rowH;
        AddRow(this, leftX, y, lblW, fieldW, "直径 (mm)", "45", out txtDiameter); y += rowH + 10;

        // -- 试验前质量 --
        AddSectionTitle(this, leftX, y, colW, "⚖ 试验前质量"); y += 30;
        AddRow(this, leftX, y, lblW, fieldW, "质量 (g)", "50.0", out txtPreWeight); y += rowH + 10;

        // ===== 右列 =====
        y = startY;

        // -- 试验参数 --
        AddSectionTitle(this, rightX, y, colW, "⚙ 试验参数"); y += 30;
        string autoTestId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        AddRow(this, rightX, y, lblW, fieldW, "试验标识", autoTestId, out txtTestId); y += rowH;
        AddRow(this, rightX, y, lblW, fieldW, "操作员", AppGlobal.Instance.CurrentUsername, out txtOperator); y += rowH;

        var lblMode = new Label { Text = "时长模式", Location = new Point(rightX, y+8), AutoSize = true, Font = new Font("Microsoft YaHei", 10), ForeColor = Color.FromArgb(80, 80, 90) };
        cmbDurationMode = new ComboBox
        {
            Location = new Point(rightX + lblW + 10, y),
            Size = new Size(fieldW, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Microsoft YaHei", 10),
            BackColor = Color.FromArgb(250, 250, 252)
        };
        cmbDurationMode.Items.AddRange(new object[] { "标准60分钟", "固定时长（自定义）" });
        cmbDurationMode.SelectedIndex = 0;
        cmbDurationMode.SelectedIndexChanged += (s, e) =>
        {
            bool show = cmbDurationMode.SelectedIndex == 1;
            lblCustomDuration.Visible = show;
            txtCustomDuration.Visible = show;
        };
        this.Controls.Add(lblMode);
        this.Controls.Add(cmbDurationMode);
        y += rowH;

        lblCustomDuration = new Label { Text = "目标时长 (秒)", Location = new Point(rightX, y+8), AutoSize = true, Font = new Font("Microsoft YaHei", 10), ForeColor = Color.FromArgb(80, 80, 90), Visible = false };
        txtCustomDuration = new TextBox { Location = new Point(rightX + lblW + 10, y), Size = new Size(fieldW, 28), Text = "600", Visible = false, Font = new Font("Microsoft YaHei", 10), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(250, 250, 252) };
        this.Controls.Add(lblCustomDuration);
        this.Controls.Add(txtCustomDuration);
        y += rowH + 10;

        // -- 设备信息 --
        AddSectionTitle(this, rightX, y, colW, "🔧 设备信息（自动获取）"); y += 30;
        AddReadOnlyRow(this, rightX, y, lblW, fieldW, "设备编号", "", out txtApparatusId); y += rowH;
        AddReadOnlyRow(this, rightX, y, lblW, fieldW, "设备名称", "", out txtApparatusName); y += rowH;
        AddReadOnlyRow(this, rightX, y, lblW, fieldW, "检定日期", "", out txtCheckDate); y += rowH;
        AddReadOnlyRow(this, rightX, y, lblW, fieldW, "恒功率值", "", out txtConstPower); y += rowH + 20;

        // ===== 按钮 =====
        btnCreate = new Button
        {
            Text = "创建试验",
            Location = new Point(rightX, y),
            Size = new Size(165, 42),
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(45, 160, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCreate.FlatAppearance.BorderSize = 0;
        btnCreate.Click += BtnCreate_Click;
        btnCreate.MouseEnter += (s, e) => btnCreate.BackColor = Color.FromArgb(35, 140, 65);
        btnCreate.MouseLeave += (s, e) => btnCreate.BackColor = Color.FromArgb(45, 160, 80);

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(rightX + 178, y),
            Size = new Size(165, 42),
            Font = new Font("Microsoft YaHei", 11),
            BackColor = Color.FromArgb(210, 210, 215),
            ForeColor = Color.FromArgb(70, 70, 80),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => this.Close();
        btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(190, 190, 195);
        btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.FromArgb(210, 210, 215);

        // 底部提示
        var lblBottom = new Label
        {
            Text = "提示：创建试验后，需在主界面点击「开始升温」来启动试验流程",
            Font = new Font("Microsoft YaHei", 8),
            ForeColor = Color.FromArgb(160, 160, 170),
            Location = new Point(leftX, y + 56),
            AutoSize = true
        };

        this.Controls.AddRange(new Control[] {
            lblTitle, lblHint, lblBottom,
            btnCreate, btnCancel
        });
    }

    private static void AddSectionTitle(Control parent, int x, int y, int w, string text)
    {
        var panel = new Panel { Location = new Point(x, y), Size = new Size(w, 32), BackColor = Color.Transparent };
        var bar = new Panel { Location = new Point(0, 6), Size = new Size(4, 22), BackColor = Color.FromArgb(45, 125, 210) };
        var lbl = new Label { Text = text, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 65), Location = new Point(12, 5), AutoSize = true };
        panel.Controls.Add(bar);
        panel.Controls.Add(lbl);
        parent.Controls.Add(panel);
    }

    private static void AddRow(Control parent, int x, int y, int lblW, int fieldW, string label, string def, out TextBox tb)
    {
        var lbl = new Label { Text = label, Location = new Point(x, y + 8), Size = new Size(lblW, 24), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei", 10), ForeColor = Color.FromArgb(80, 80, 90) };
        tb = new TextBox { Location = new Point(x + lblW + 10, y), Size = new Size(fieldW, 28), Text = def, Font = new Font("Microsoft YaHei", 10), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(250, 250, 252) };
        parent.Controls.Add(lbl);
        parent.Controls.Add(tb);
    }

    private static void AddReadOnlyRow(Control parent, int x, int y, int lblW, int fieldW, string label, string def, out TextBox tb)
    {
        AddRow(parent, x, y, lblW, fieldW, label, def, out tb);
        tb.ReadOnly = true;
        tb.BackColor = Color.FromArgb(238, 238, 242);
        tb.ForeColor = Color.FromArgb(130, 130, 140);
    }

    private void LoadDeviceInfo()
    {
        var apparatus = AppGlobal.Instance.CurrentApparatus;
        if (apparatus != null)
        {
            txtApparatusId.Text = apparatus.InnerNumber;
            txtApparatusName.Text = apparatus.ApparatusName;
            txtCheckDate.Text = apparatus.CheckDateF.ToString("yyyy-MM-dd");
            txtConstPower.Text = (apparatus.ConstPower ?? 2048).ToString();
        }
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtProductId.Text)) { MessageBox.Show("请输入样品编号"); txtProductId.Focus(); return; }
        if (string.IsNullOrWhiteSpace(txtProductName.Text)) { MessageBox.Show("请输入样品名称"); txtProductName.Focus(); return; }
        if (!double.TryParse(txtPreWeight.Text, out double pw) || pw <= 0) { MessageBox.Show("请输入有效的试验前质量"); txtPreWeight.Focus(); return; }
        if (!double.TryParse(txtAmbTemp.Text, out double at)) { MessageBox.Show("请输入有效的环境温度"); txtAmbTemp.Focus(); return; }
        if (!double.TryParse(txtAmbHumi.Text, out double ah)) { MessageBox.Show("请输入有效的环境湿度"); txtAmbHumi.Focus(); return; }
        if (!double.TryParse(txtHeight.Text, out double h) || h <= 0) { MessageBox.Show("请输入有效的高度"); txtHeight.Focus(); return; }
        if (!double.TryParse(txtDiameter.Text, out double d) || d <= 0) { MessageBox.Show("请输入有效的直径"); txtDiameter.Focus(); return; }

        try
        {
            var db = AppGlobal.Instance.Db;
            var app = AppGlobal.Instance.CurrentApparatus;
            db.InsertProduct(new ProductMaster { ProductId = txtProductId.Text.Trim(), ProductName = txtProductName.Text.Trim(), Specific = txtSpecific.Text.Trim(), Height = h, Diameter = d });
            string tid = txtTestId.Text.Trim();
            db.InsertTestMaster(new TestMasterRecord
            {
                ProductId = txtProductId.Text.Trim(), TestId = tid, TestDate = DateTime.Now,
                AmbTemp = at, AmbHumi = ah, According = "ISO 11820:2022", Operator = txtOperator.Text.Trim(),
                ApparatusId = app?.InnerNumber ?? "FURNACE-01", ApparatusName = app?.ApparatusName ?? "一号试验炉",
                ApparatusChkDate = app?.CheckDateF ?? DateTime.Now, RptNo = txtProductId.Text.Trim(), PreWeight = pw
            });
            TestCreated = true; ProductId = txtProductId.Text.Trim(); ProductNameVal = txtProductName.Text.Trim(); TestId = tid;
            this.DialogResult = DialogResult.OK; this.Close();
        }
        catch (Exception ex) { MessageBox.Show($"创建失败：{ex.Message}"); }
    }

    public TestDurationMode GetDurationMode() => cmbDurationMode.SelectedIndex == 0 ? TestDurationMode.Standard60Min : TestDurationMode.FixedDuration;
    public int GetTargetDuration() => int.TryParse(txtCustomDuration.Text, out int v) ? v : 600;
    public double GetPreWeight() => double.TryParse(txtPreWeight.Text, out double v) ? v : 50;
    public double GetAmbTemp() => double.TryParse(txtAmbTemp.Text, out double v) ? v : 25;
    public double GetAmbHumi() => double.TryParse(txtAmbHumi.Text, out double v) ? v : 50;
    public string GetOperatorName() => txtOperator.Text.Trim();
}