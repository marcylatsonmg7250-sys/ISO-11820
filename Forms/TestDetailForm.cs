using ISO11820.Models;

#nullable disable warnings

namespace ISO11820.Forms;

/// <summary>
/// 试验详情查看窗体
/// </summary>
public partial class TestDetailForm : Form
{
#nullable disable
    private readonly TestMasterRecord _record;
    private readonly Services.ExportService? _exportService;

    public TestDetailForm(TestMasterRecord record, Services.ExportService? exportService = null)
    {
        _record = record;
        _exportService = exportService;
        InitializeComponent();
        PopulateData();
    }

    private TextBox txtInfo;
    private Button btnExportExcel;
    private Button btnExportPdf;
    private Button btnClose;

    private void InitializeComponent()
    {
        this.Text = "试验详情";
        this.Size = new Size(550, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        txtInfo = new TextBox
        {
            Location = new Point(15, 15),
            Size = new Size(500, 350),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 10)
        };

        btnExportExcel = new Button
        {
            Text = "导出Excel",
            Location = new Point(80, 380),
            Size = new Size(110, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9)
        };
        btnExportExcel.FlatAppearance.BorderSize = 0;
        btnExportExcel.Click += BtnExportExcel_Click;

        btnExportPdf = new Button
        {
            Text = "导出PDF",
            Location = new Point(210, 380),
            Size = new Size(110, 35),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9)
        };
        btnExportPdf.FlatAppearance.BorderSize = 0;
        btnExportPdf.Click += BtnExportPdf_Click;

        btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(340, 380),
            Size = new Size(110, 35),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9)
        };
        btnClose.Click += (s, e) => this.Close();

        this.Controls.AddRange(new Control[] { txtInfo, btnExportExcel, btnExportPdf, btnClose });
    }

    private void PopulateData()
    {
        bool passed = _record.DeltaTf <= 50 && _record.LostWeightPer <= 50 && _record.FlameDuration < 5;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("══════════════ ISO 11820 试验报告 ══════════════");
        sb.AppendLine();
        sb.AppendLine($"  样品编号:      {_record.ProductId}");
        sb.AppendLine($"  试验ID:        {_record.TestId}");
        sb.AppendLine($"  试验日期:      {_record.TestDate:yyyy-MM-dd}");
        sb.AppendLine($"  操作员:        {_record.Operator}");
        sb.AppendLine($"  环境温度:      {_record.AmbTemp:F1} °C");
        sb.AppendLine($"  环境湿度:      {_record.AmbHumi:F1} %");
        sb.AppendLine($"  设备:          {_record.ApparatusName}");
        sb.AppendLine($"  试验依据:      {_record.According}");
        sb.AppendLine();
        sb.AppendLine("──────────────── 质量数据 ─────────────────");
        sb.AppendLine($"  试验前质量:    {_record.PreWeight:F2} g");
        sb.AppendLine($"  试验后质量:    {_record.PostWeight:F2} g");
        sb.AppendLine($"  失重量:        {_record.LostWeight:F2} g");
        sb.AppendLine($"  失重率:        {_record.LostWeightPer:F2} %");
        sb.AppendLine();
        sb.AppendLine("──────────────── 温度数据 ─────────────────");
        sb.AppendLine($"  炉温1温升:     {_record.DeltaTf1:F2} °C");
        sb.AppendLine($"  炉温2温升:     {_record.DeltaTf2:F2} °C");
        sb.AppendLine($"  表面温升:      {_record.DeltaTs:F2} °C");
        sb.AppendLine($"  中心温升:      {_record.DeltaTc:F2} °C");
        sb.AppendLine($"  样品温升(ΔT):  {_record.DeltaTf:F2} °C");
        sb.AppendLine();
        sb.AppendLine($"  炉温1最大值:   {_record.MaxTf1:F2} °C (第{_record.MaxTf1Time}秒)");
        sb.AppendLine($"  炉温2最大值:   {_record.MaxTf2:F2} °C (第{_record.MaxTf2Time}秒)");
        sb.AppendLine($"  表面温最大值:  {_record.MaxTs:F2} °C (第{_record.MaxTsTime}秒)");
        sb.AppendLine($"  中心温最大值:  {_record.MaxTc:F2} °C (第{_record.MaxTcTime}秒)");
        sb.AppendLine();
        sb.AppendLine("──────────────── 试验过程 ─────────────────");
        sb.AppendLine($"  试验时长:      {_record.TotalTestTime} 秒");
        sb.AppendLine($"  恒功率值:      {_record.ConstPower}");
        sb.AppendLine($"  火焰持续时间:  {_record.FlameDuration} 秒");
        sb.AppendLine($"  备注:          {_record.Memo ?? "无"}");
        sb.AppendLine();
        sb.AppendLine("──────────────── 判定结论 ─────────────────");
        string result = passed ? "✅ 通过" : "❌ 不通过";
        sb.AppendLine($"  结论:          {result}");
        sb.AppendLine($"  判定标准:      ΔT≤50°C, 失重率≤50%, 火焰<5s");
        sb.AppendLine("══════════════════════════════════════════════");

        txtInfo.Text = sb.ToString();
    }

    private void BtnExportExcel_Click(object? sender, EventArgs e)
    {
        if (_exportService == null)
        {
            MessageBox.Show("导出服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            string path = _exportService.ExportExcel(_record);
            MessageBox.Show($"Excel报告已生成：\n{path}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        if (_exportService == null)
        {
            MessageBox.Show("导出服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            string path = _exportService.ExportPdf(_record);
            if (!string.IsNullOrEmpty(path))
                MessageBox.Show($"PDF报告已生成：\n{path}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}