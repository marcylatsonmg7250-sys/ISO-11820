namespace ISO11820.Forms;

#nullable disable warnings

/// <summary>
/// 试验现象记录窗体
/// </summary>
public partial class TestRecordForm : Form
{
#nullable disable
    private CheckBox chkFlame;
    private TextBox txtFlameTime;
    private TextBox txtFlameDuration;
    private TextBox txtPostWeight;
    private TextBox txtMemo;
    private Button btnSave;
    private Button btnCancel;
    private Label lblFlameTime;
    private Label lblFlameDuration;

    public bool Saved { get; private set; }
    public bool HasFlame => chkFlame.Checked;
    public int FlameTime => int.TryParse(txtFlameTime.Text, out int v) ? v : 0;
    public int FlameDuration => int.TryParse(txtFlameDuration.Text, out int v) ? v : 0;
    public double PostWeight => double.TryParse(txtPostWeight.Text, out double v) ? v : 0;
    public string Memo => txtMemo.Text.Trim();

    public TestRecordForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "试验现象记录";
        this.Size = new Size(400, 380);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        int y = 20;
        int leftLabel = 20;
        int leftValue = 160;

        // 火焰
        chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(leftLabel, y),
            AutoSize = true,
            Font = new Font("Microsoft YaHei", 10)
        };
        chkFlame.CheckedChanged += ChkFlame_CheckedChanged;
        y += 35;

        lblFlameTime = new Label { Text = "火焰发生时刻(秒):", Location = new Point(leftLabel, y), AutoSize = true, Enabled = false };
        txtFlameTime = new TextBox { Location = new Point(leftValue, y), Size = new Size(200, 25), Text = "0", Enabled = false };
        y += 32;

        lblFlameDuration = new Label { Text = "火焰持续时间(秒):", Location = new Point(leftLabel, y), AutoSize = true, Enabled = false };
        txtFlameDuration = new TextBox { Location = new Point(leftValue, y), Size = new Size(200, 25), Text = "0", Enabled = false };
        y += 40;

        // 试验后质量
        var lblPostWeight = new Label { Text = "试验后质量(g):", Location = new Point(leftLabel, y), AutoSize = true, Font = new Font("Microsoft YaHei", 10) };
        txtPostWeight = new TextBox { Location = new Point(leftValue, y), Size = new Size(200, 25), Text = "" };
        var lblRequired = new Label { Text = "*必填", Location = new Point(leftValue + 210, y), AutoSize = true, ForeColor = Color.Red };
        y += 35;

        // 备注
        var lblMemo = new Label { Text = "备注:", Location = new Point(leftLabel, y), AutoSize = true };
        txtMemo = new TextBox { Location = new Point(leftValue, y), Size = new Size(200, 60), Multiline = true, ScrollBars = ScrollBars.Vertical };
        y += 80;

        // 按钮
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(80, y),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(200, y),
            Size = new Size(100, 35),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10)
        };
        btnCancel.Click += (s, e) => this.Close();

        this.Controls.AddRange(new Control[] {
            chkFlame, lblFlameTime, txtFlameTime, lblFlameDuration, txtFlameDuration,
            lblPostWeight, txtPostWeight, lblRequired, lblMemo, txtMemo,
            btnSave, btnCancel
        });
    }

    private void ChkFlame_CheckedChanged(object? sender, EventArgs e)
    {
        bool enabled = chkFlame.Checked;
        lblFlameTime.Enabled = enabled;
        txtFlameTime.Enabled = enabled;
        lblFlameDuration.Enabled = enabled;
        txtFlameDuration.Enabled = enabled;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPostWeight.Text))
        {
            MessageBox.Show("请输入试验后质量", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPostWeight.Focus();
            return;
        }
        if (!double.TryParse(txtPostWeight.Text, out double postWeight) || postWeight <= 0)
        {
            MessageBox.Show("请输入有效的试验后质量", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPostWeight.Focus();
            return;
        }

        Saved = true;
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}