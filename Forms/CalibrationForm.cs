using ISO11820.Core;
using ISO11820.Models;

#nullable disable warnings

namespace ISO11820.Forms;

/// <summary>
/// 设备校准 Tab页内容
/// </summary>
public partial class CalibrationForm : Form
{
#nullable disable
    private DataGridView dgvRecords;
    private Button btnRecord;
    private Button btnRefresh;
    private TextBox txtCalTemp;

    public CalibrationForm()
    {
        InitializeComponent();
        LoadRecords();
    }

    private void InitializeComponent()
    {
        this.Text = "设备校准";
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(10, 10, 26);

        var bgPanel = Color.FromArgb(16, 16, 36);
        var bgCard = Color.FromArgb(20, 20, 42);
        var borderColor = Color.FromArgb(42, 42, 58);
        var textPri = Color.FromArgb(224, 224, 224);
        var textSec = Color.FromArgb(112, 112, 128);
        var accentTeal = Color.FromArgb(0, 255, 157);
        var accentBlue = Color.FromArgb(0, 191, 255);
        var accentOrange = Color.FromArgb(255, 157, 0);

        int contentW = 780;

        // 内容容器 — 居中
        var container = new Panel
        {
            Width = contentW,
            Height = 500,
            BackColor = Color.Transparent
        };
        this.Resize += (s, e) =>
        {
            container.Left = Math.Max(0, (this.Width - contentW) / 2);
        };

        // ---- 顶部状态栏 ----
        var topBar = new Panel
        {
            Location = new Point(0, 12),
            Size = new Size(contentW, 56),
            BackColor = bgPanel
        };

        var lblCalLabel = new Label
        {
            Text = "当前校准温度:",
            Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            ForeColor = textSec,
            Location = new Point(24, 18),
            AutoSize = true
        };

        txtCalTemp = new TextBox
        {
            Location = new Point(190, 10),
            Size = new Size(165, 36),
            Font = new Font("Consolas", 16, FontStyle.Bold),
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = accentTeal,
            TextAlign = HorizontalAlignment.Center,
            Text = "0.0 °C",
            BorderStyle = BorderStyle.FixedSingle
        };

        btnRecord = new Button
        {
            Text = "● 记录校准数据",
            Location = new Point(375, 12),
            Size = new Size(150, 34),
            BackColor = accentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRecord.FlatAppearance.BorderSize = 0;
        btnRecord.Click += BtnRecord_Click;
        btnRecord.MouseEnter += (s, e) => btnRecord.BackColor = Color.FromArgb(0, 140, 230);
        btnRecord.MouseLeave += (s, e) => btnRecord.BackColor = accentBlue;

        btnRefresh = new Button
        {
            Text = "↻ 刷新",
            Location = new Point(540, 12),
            Size = new Size(90, 34),
            BackColor = accentOrange,
            ForeColor = Color.FromArgb(10, 10, 26),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (s, e) => LoadRecords();
        btnRefresh.MouseEnter += (s, e) => btnRefresh.BackColor = Color.FromArgb(200, 130, 30);
        btnRefresh.MouseLeave += (s, e) => btnRefresh.BackColor = accentOrange;

        topBar.Controls.AddRange(new Control[] { lblCalLabel, txtCalTemp, btnRecord, btnRefresh });

        // ---- 历史记录表 ----
        dgvRecords = new DataGridView
        {
            Location = new Point(0, 78),
            Size = new Size(contentW, 400),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.FromArgb(10, 10, 26),
            BorderStyle = BorderStyle.None,
            GridColor = borderColor,
            EnableHeadersVisualStyles = false
        };

        // 表头样式
        dgvRecords.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = bgPanel,
            ForeColor = accentTeal,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            SelectionBackColor = bgPanel,
            SelectionForeColor = accentTeal
        };
        dgvRecords.ColumnHeadersHeight = 34;
        dgvRecords.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // 单元格样式
        dgvRecords.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = bgCard,
            ForeColor = textPri,
            Font = new Font("Microsoft YaHei", 9),
            SelectionBackColor = Color.FromArgb(0, 100, 180),
            SelectionForeColor = Color.White,
            Padding = new Padding(4, 2, 4, 2)
        };

        // 交替行
        dgvRecords.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(24, 24, 48),
            ForeColor = textPri,
            Font = new Font("Microsoft YaHei", 9),
            SelectionBackColor = Color.FromArgb(0, 100, 180),
            SelectionForeColor = Color.White
        };

        dgvRecords.RowTemplate.Height = 28;
        dgvRecords.RowHeadersVisible = false;

        container.Controls.AddRange(new Control[] { topBar, dgvRecords });
        this.Controls.Add(container);

        container.Left = Math.Max(0, (this.Width - contentW) / 2);
    }

    public void UpdateCalibrationTemperature(double temp)
    {
        if (txtCalTemp.InvokeRequired)
        {
            txtCalTemp.Invoke(() => txtCalTemp.Text = $"{temp:F1} °C");
        }
        else
        {
            txtCalTemp.Text = $"{temp:F1} °C";
        }
    }

    private void LoadRecords()
    {
        try
        {
            var records = AppGlobal.Instance.Db.GetCalibrationRecords();
            dgvRecords.DataSource = records.Select(r => new
            {
                日期 = r.CalibrationDate,
                类型 = r.CalibrationType,
                操作员 = r.Operator,
                平均温度 = r.AverageTemperature?.ToString("F1") ?? "-",
                最大偏差 = r.MaxDeviation?.ToString("F2") ?? "-",
                是否通过 = r.PassedCriteria == 1 ? "是" : "否",
                备注 = r.Remarks
            }).ToList();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "加载校准记录失败");
        }
    }

    private void BtnRecord_Click(object? sender, EventArgs e)
    {
        try
        {
            var record = new CalibrationRecord
            {
                Id = Guid.NewGuid().ToString(),
                CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CalibrationType = "Surface",
                ApparatusId = AppGlobal.Instance.CurrentApparatus?.ApparatusId ?? 0,
                Operator = AppGlobal.Instance.CurrentUsername,
                TemperatureData = "{}",
                PassedCriteria = 1,
                Remarks = "手动记录",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            AppGlobal.Instance.Db.InsertCalibrationRecord(record);
            LoadRecords();
            MessageBox.Show("校准记录已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}