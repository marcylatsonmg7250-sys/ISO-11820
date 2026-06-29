using ISO11820.Core;
using ISO11820.Models;
using ISO11820.Services;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.WindowsForms;
using System.Drawing.Imaging;

#nullable disable warnings

namespace ISO11820.Forms;

/// <summary>
/// 主窗体
/// </summary>
public partial class MainForm : Form
{
    #region 配色方案
    private static readonly Color BgMain = Color.FromArgb(10, 10, 26);
    private static readonly Color BgPanel = Color.FromArgb(16, 16, 36);
    private static readonly Color BgCard = Color.FromArgb(20, 20, 42);
    private static readonly Color BgButton = Color.FromArgb(26, 26, 50);
    private static readonly Color BgButtonHover = Color.FromArgb(42, 42, 72);
    private static readonly Color BorderColor = Color.FromArgb(42, 42, 58);
    private static readonly Color TextPrimary = Color.FromArgb(224, 224, 224);
    private static readonly Color TextSecondary = Color.FromArgb(112, 112, 128);
    private static readonly Color AccentTeal = Color.FromArgb(0, 255, 157);
    private static readonly Color AccentOrange = Color.FromArgb(255, 157, 0);
    private static readonly Color AccentBlue = Color.FromArgb(0, 191, 255);
    private static readonly Color AccentGreen = Color.FromArgb(0, 230, 120);
    private static readonly Color AccentRed = Color.FromArgb(255, 80, 80);
    #endregion

#nullable disable
    // 核心对象
    private readonly TestMaster _testMaster;
    private readonly DaqWorker _daqWorker;
    private readonly ExportService _exportService;

    // Tab控件
    private TabControl tabControl;
    private TabPage tabTest;
    private TabPage tabQuery;
    private TabPage tabCalibration;

    // ===== 试验控制 Tab =====
    // 温度显示
    private Label lblTf1, lblTf2, lblTs, lblTc, lblTCal;
    // 曲线图
    private PlotView plotView;
    private PlotModel plotModel;
    private LineSeries seriesTf1, seriesTf2, seriesTs, seriesTc;
    // 信息面板
    private Label lblStatus, lblTimer, lblDrift, lblProductId;
    // 按钮
    private Button btnNewTest, btnStartHeat, btnStopHeat, btnStartRecord, btnStopRecord, btnTestRecord;
    // 系统消息
    private RichTextBox rtbMessages;

    // 布局容器
    private TableLayoutPanel layoutMain;
    private Panel sidebarPanel;
    private FlowLayoutPanel tempCardsPanel;

    // ===== 记录查询 Tab =====
    private DateTimePicker dtpFrom, dtpTo;
    private TextBox txtSearchProductId;
    private ComboBox cmbFilterOperator;
    private Button btnSearch, btnExportQuery;
    private DataGridView dgvRecords;

    // ===== 设备校准 Tab =====
    private CalibrationForm? _calibrationForm;

    public MainForm()
    {
        _testMaster = new TestMaster();
        _daqWorker = new DaqWorker(_testMaster);
        _exportService = new ExportService(Core.AppGlobal.Instance.Db);

        InitializeComponent();
        SetupPlot();
        SubscribeEvents();

        this.Text = $"ISO 11820 不燃性试验系统 - 操作员: {Core.AppGlobal.Instance.CurrentUsername}";
        this.FormClosing += MainForm_FormClosing;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _daqWorker.Stop();
        _daqWorker.Dispose();
    }

    #region 初始化UI

    private void InitializeComponent()
    {
        this.Size = new Size(1320, 820);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1024, 700);
        this.BackColor = BgMain;

        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 10),
            DrawMode = TabDrawMode.OwnerDrawFixed
        };
        tabControl.DrawItem += (s, e) =>
        {
            var tab = tabControl.TabPages[e.Index];
            var rect = tabControl.GetTabRect(e.Index);
            bool selected = tabControl.SelectedIndex == e.Index;

            using var bgBrush = new SolidBrush(selected ? BgMain : Color.FromArgb(22, 22, 42));
            e.Graphics.FillRectangle(bgBrush, rect);

            using var textBrush = new SolidBrush(selected ? AccentTeal : TextSecondary);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tab.Text, tabControl.Font, textBrush, rect, sf);
        };

        tabTest = new TabPage("试验控制");
        tabQuery = new TabPage("记录查询");
        tabCalibration = new TabPage("设备校准");

        BuildTestTab();
        BuildQueryTab();
        BuildCalibrationTab();

        tabControl.TabPages.AddRange(new[] { tabTest, tabQuery, tabCalibration });
        this.Controls.Add(tabControl);
    }

    private void BuildTestTab()
    {
        tabTest.BackColor = BgMain;

        // 主布局：左侧边栏(220px) + 右侧内容区
        layoutMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            BackColor = BgMain
        };
        layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // ===== 左侧边栏 =====
        sidebarPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgPanel,
            Padding = new Padding(10)
        };

        // 侧边栏标题
        var sidebarTitle = new Label
        {
            Text = "操作面板",
            ForeColor = TextPrimary,
            Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
            Location = new Point(10, 12),
            AutoSize = true
        };

        // 分割线
        var divider = new Label
        {
            Text = "",
            Location = new Point(10, 38),
            Size = new Size(194, 1),
            BackColor = BorderColor
        };

        // 按钮列表（竖排，带图标）
        int btnY = 50;
        const int btnH = 38;
        const int btnSpacing = 8;

        btnNewTest = CreateSidebarButton("+  新建试验", ref btnY, btnH, btnSpacing, AccentTeal);
        btnStartHeat = CreateSidebarButton("▶  开始升温", ref btnY, btnH, btnSpacing, AccentOrange);
        btnStopHeat = CreateSidebarButton("■  停止升温", ref btnY, btnH, btnSpacing, AccentOrange);
        btnStartRecord = CreateSidebarButton("●  开始记录", ref btnY, btnH, btnSpacing, AccentGreen);
        btnStopRecord = CreateSidebarButton("■  停止记录", ref btnY, btnH, btnSpacing, AccentRed);
        btnTestRecord = CreateSidebarButton("📋  试验记录", ref btnY, btnH, btnSpacing, AccentBlue);

        btnNewTest.Click += BtnNewTest_Click;
        btnStartHeat.Click += BtnStartHeat_Click;
        btnStopHeat.Click += BtnStopHeat_Click;
        btnStartRecord.Click += BtnStartRecord_Click;
        btnStopRecord.Click += BtnStopRecord_Click;
        btnTestRecord.Click += BtnTestRecord_Click;

        // ---- 信息面板（侧边栏底部） ----
        var infoY = btnY + 16;
        var infoTitle = new Label
        {
            Text = "实时信息",
            ForeColor = TextSecondary,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Location = new Point(10, infoY),
            AutoSize = true
        };
        infoY += 22;

        var divider2 = new Label
        {
            Text = "",
            Location = new Point(10, infoY + 2),
            Size = new Size(188, 1),
            BackColor = BorderColor
        };
        infoY += 10;

        lblStatus = CreateSidebarInfoLabel("状态: 空闲", ref infoY);
        lblTimer = CreateSidebarInfoLabel("计时: 0 秒", ref infoY);
        lblDrift = CreateSidebarInfoLabel("温漂: 0.00 °C/10min", ref infoY);
        lblProductId = CreateSidebarInfoLabel("样品: -", ref infoY);

        sidebarPanel.Controls.AddRange(new Control[] {
            sidebarTitle, divider,
            btnNewTest, btnStartHeat, btnStopHeat,
            btnStartRecord, btnStopRecord, btnTestRecord,
            infoTitle, divider2,
            lblStatus, lblTimer, lblDrift, lblProductId
        });

        // ===== 右侧内容区 =====
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgMain,
            Padding = new Padding(8, 0, 0, 0)
        };

        // 温度卡片行（FlowLayoutPanel 自动排列）
        tempCardsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(1000, 126),
            BackColor = BgMain,
            Padding = new Padding(0, 8, 0, 4),
            AutoScroll = false
        };

        lblTf1 = CreateTempCardLabel("炉温1");
        lblTf2 = CreateTempCardLabel("炉温2");
        lblTs = CreateTempCardLabel("表面温");
        lblTc = CreateTempCardLabel("中心温");
        lblTCal = CreateTempCardLabel("校准温");

        tempCardsPanel.Controls.Add(CreateTempCard("炉温1", lblTf1));
        tempCardsPanel.Controls.Add(CreateTempCard("炉温2", lblTf2));
        tempCardsPanel.Controls.Add(CreateTempCard("表面温", lblTs));
        tempCardsPanel.Controls.Add(CreateTempCard("中心温", lblTc));
        tempCardsPanel.Controls.Add(CreateTempCard("校准温", lblTCal));

        // 曲线图
        plotView = new PlotView
        {
            Location = new Point(0, 134),
            Size = new Size(1000, 340),
            BackColor = BgPanel,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };

        // 系统消息区域
        var msgHeader = new Label
        {
            Text = "系统消息",
            ForeColor = TextSecondary,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Location = new Point(4, 480),
            AutoSize = true
        };

        rtbMessages = new RichTextBox
        {
            Location = new Point(0, 502),
            Size = new Size(1000, 190),
            BackColor = BgPanel,
            ForeColor = TextPrimary,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
        };

        contentPanel.Controls.AddRange(new Control[] { tempCardsPanel, plotView, msgHeader, rtbMessages });

        // 组装到 TableLayoutPanel
        layoutMain.Controls.Add(sidebarPanel, 0, 0);
        layoutMain.Controls.Add(contentPanel, 1, 0);

        tabTest.Controls.Add(layoutMain);

        // 窗口大小变化时调整右侧控件宽度
        tabTest.Resize += (s, e) =>
        {
            int rightWidth = tabTest.Width - 252;
            if (rightWidth < 400) rightWidth = 400;
            tempCardsPanel.Width = rightWidth;
            plotView.Width = rightWidth;
            rtbMessages.Width = rightWidth;
        };

        UpdateButtonStates();
    }

    #region 侧边栏控件创建

    private Button CreateSidebarButton(string text, ref int y, int height, int spacing, Color accentColor)
    {
        bool isRecordingBtn = text.Contains("停止记录");
        var btn = new Button
        {
            Text = text,
            Location = new Point(8, y),
            Size = new Size(196, height),
            Font = new Font("Microsoft YaHei", 10),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = isRecordingBtn ? BgButton : BgButton,
            ForeColor = TextPrimary,
            Enabled = false,
            Cursor = Cursors.Hand,
            Tag = accentColor
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderColor;

        btn.MouseEnter += (s, e) =>
        {
            if (btn.Enabled)
                btn.BackColor = BgButtonHover;
        };
        btn.MouseLeave += (s, e) =>
        {
            btn.BackColor = BgButton;
        };
        btn.EnabledChanged += (s, e) =>
        {
            btn.ForeColor = btn.Enabled ? TextPrimary : TextSecondary;
            btn.FlatAppearance.BorderColor = btn.Enabled ? BorderColor : Color.FromArgb(30, 30, 44);
        };

        y += height + spacing;
        return btn;
    }

    private Label CreateSidebarInfoLabel(string text, ref int y)
    {
        var lbl = new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Microsoft YaHei", 9),
            Location = new Point(10, y),
            AutoSize = true
        };
        y += 24;
        return lbl;
    }

    #endregion

    #region 温度卡片创建

    private Label CreateTempCardLabel(string name)
    {
        return new Label
        {
            Text = "0.0 °C",
            ForeColor = GetChannelColor(name),
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 24, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private Panel CreateTempCard(string title, Label valueLabel)
    {
        var card = new Panel
        {
            Size = new Size(185, 108),
            BackColor = BgCard,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 10, 0)
        };

        // 绘制卡片边框
        card.Paint += (s, e) =>
        {
            var c = s as Panel;
            if (c == null) return;
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        };

        // 标题放最顶部
        var titleLabel = new Label
        {
            Text = title,
            ForeColor = TextSecondary,
            Font = new Font("Microsoft YaHei", 8),
            Location = new Point(12, 6),
            AutoSize = true
        };

        // 数值放在标题下方，确保不重叠
        valueLabel.Location = new Point(4, 44);
        valueLabel.Size = new Size(176, 58);

        card.Controls.Add(titleLabel);
        card.Controls.Add(valueLabel);
        return card;
    }

    private Color GetChannelColor(string name)
    {
        return name switch
        {
            "炉温1" => Color.FromArgb(255, 100, 100),
            "炉温2" => Color.FromArgb(255, 180, 80),
            "表面温" => Color.FromArgb(80, 210, 255),
            "中心温" => Color.FromArgb(80, 255, 140),
            "校准温" => Color.FromArgb(255, 210, 100),
            _ => Color.White
        };
    }

    #endregion

    #endregion

    #region 曲线图初始化

    private void SetupPlot()
    {
        plotModel = new PlotModel
        {
            Title = "温度曲线",
            TitleColor = OxyColor.FromRgb(200, 200, 200),
            PlotAreaBorderColor = OxyColor.FromRgb(60, 60, 80),
            TextColor = OxyColor.FromRgb(200, 200, 200),
            Background = OxyColor.FromRgb(16, 16, 36)
        };

        // X轴：时间（秒），滚动显示最近10分钟
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            TitleColor = OxyColor.FromRgb(180, 180, 180),
            TextColor = OxyColor.FromRgb(180, 180, 180),
            AxislineColor = OxyColor.FromRgb(80, 80, 100),
            TicklineColor = OxyColor.FromRgb(80, 80, 100),
            Minimum = 0,
            Maximum = 600,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(50, 50, 65)
        });

        // Y轴：温度 0~800°C
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            TitleColor = OxyColor.FromRgb(180, 180, 180),
            TextColor = OxyColor.FromRgb(180, 180, 180),
            AxislineColor = OxyColor.FromRgb(80, 80, 100),
            TicklineColor = OxyColor.FromRgb(80, 80, 100),
            Minimum = 0,
            Maximum = 800,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(50, 50, 65)
        });

        // 目标温度线：750°C 橙色虚线
        plotModel.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
        {
            Y = 750,
            Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
            Color = OxyColor.FromRgb(255, 157, 0),
            LineStyle = LineStyle.Dash,
            StrokeThickness = 1.5,
            Text = "目标 750°C",
            TextColor = OxyColor.FromRgb(255, 157, 0),
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
            TextVerticalAlignment = OxyPlot.VerticalAlignment.Top,
            FontSize = 10
        });

        // 稳定区间：745~755°C 半透明绿色带
        plotModel.Annotations.Add(new OxyPlot.Annotations.RectangleAnnotation
        {
            MinimumY = 745,
            MaximumY = 755,
            Fill = OxyColor.FromArgb(20, 0, 255, 100),
            Text = "稳定区间 745-755°C",
            TextColor = OxyColor.FromArgb(80, 0, 200, 80),
            FontSize = 9,
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right,
            TextVerticalAlignment = OxyPlot.VerticalAlignment.Top
        });

        // 4条曲线
        seriesTf1 = CreateLineSeries("炉温1", OxyColor.FromRgb(0, 255, 157));       // 青色
        seriesTf2 = CreateLineSeries("炉温2", OxyColor.FromRgb(0, 191, 255));       // 蓝色
        seriesTs = CreateLineSeries("表面温", OxyColor.FromRgb(255, 200, 80));       // 金色
        seriesTc = CreateLineSeries("中心温", OxyColor.FromRgb(255, 120, 160));     // 粉色

        plotModel.Series.Add(seriesTf1);
        plotModel.Series.Add(seriesTf2);
        plotModel.Series.Add(seriesTs);
        plotModel.Series.Add(seriesTc);

        plotView.Model = plotModel;
    }

    private LineSeries CreateLineSeries(string title, OxyColor color)
    {
        return new LineSeries
        {
            Title = title,
            Color = color,
            StrokeThickness = 1.5,
            MarkerType = MarkerType.None,
            TrackerFormatString = "{Title}: {2:F1} °C\n时间: {0:F0} 秒"
        };
    }

    private int _plotTimeCounter;
    private void UpdatePlot(double tf1, double tf2, double ts, double tc)
    {
        _plotTimeCounter++;

        seriesTf1.Points.Add(new DataPoint(_plotTimeCounter, tf1));
        seriesTf2.Points.Add(new DataPoint(_plotTimeCounter, tf2));
        seriesTs.Points.Add(new DataPoint(_plotTimeCounter, ts));
        seriesTc.Points.Add(new DataPoint(_plotTimeCounter, tc));

        // 滚动显示最近600秒
        double xMin = Math.Max(0, _plotTimeCounter - 600);
        double xMax = Math.Max(600, _plotTimeCounter);
        plotModel.Axes[0].Minimum = xMin;
        plotModel.Axes[0].Maximum = xMax;

        // 限制点数
        const int maxPoints = 2000;
        if (seriesTf1.Points.Count > maxPoints)
        {
            int remove = seriesTf1.Points.Count - maxPoints;
            for (int i = 0; i < remove; i++)
            {
                seriesTf1.Points.RemoveAt(0);
                seriesTf2.Points.RemoveAt(0);
                seriesTs.Points.RemoveAt(0);
                seriesTc.Points.RemoveAt(0);
            }
        }

        plotView.InvalidatePlot(true);
    }

    #endregion

    #region 事件订阅

    private void SubscribeEvents()
    {
        _testMaster.DataBroadcast += OnDataBroadcast;
        _testMaster.StateChanged += OnStateChanged;
        _daqWorker.Start();
    }

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(() => OnDataBroadcast(sender, e));
            return;
        }

        // 更新温度显示
        UpdateTemperatureDisplay(e.Temperatures);

        // 更新图表
        double tf1 = e.Temperatures.GetValueOrDefault(0, 0);
        double tf2 = e.Temperatures.GetValueOrDefault(1, 0);
        double ts = e.Temperatures.GetValueOrDefault(2, 0);
        double tc = e.Temperatures.GetValueOrDefault(3, 0);
        UpdatePlot(tf1, tf2, ts, tc);

        // 更新状态和信息
        lblStatus.Text = $"状态: {e.Status}";
        lblTimer.Text = $"计时: {e.ElapsedSeconds} 秒";
        lblDrift.Text = $"温漂: {e.TemperatureDrift:F2} °C/10min";
        lblProductId.Text = $"样品: {_testMaster.CurrentProductId ?? "-"}";

        // 录制中显示已记录点数
        if (e.CurrentState == TestState.Recording && e.RecordedCount > 0)
            lblStatus.Text = $"状态: {e.Status} (已记录{e.RecordedCount}条)";
        else
            lblStatus.Text = $"状态: {e.Status}";

        // 更新校准温度
        double tcal = e.Temperatures.GetValueOrDefault(16, 0);
        _calibrationForm?.UpdateCalibrationTemperature(tcal);

        // 更新系统消息（带彩色圆点标记）
        foreach (var msg in e.Messages)
        {
            Color color = msg.Message.Contains("终止") ? AccentOrange :
                          msg.Message.Contains("错误") ? AccentRed :
                          msg.Message.Contains("开始") ? AccentGreen :
                          msg.Message.Contains("稳定") ? AccentBlue :
                          TextPrimary;

            rtbMessages.SelectionStart = rtbMessages.TextLength;
            rtbMessages.SelectionLength = 0;

            // 彩色圆点
            rtbMessages.SelectionColor = color;
            rtbMessages.AppendText("● ");
            rtbMessages.SelectionColor = TextSecondary;
            rtbMessages.AppendText($"{msg.Time}  ");
            rtbMessages.SelectionColor = TextPrimary;
            rtbMessages.AppendText($"{msg.Message}\n");
            rtbMessages.ScrollToCaret();
        }
    }

    private void UpdateTemperatureDisplay(Dictionary<int, double> temps)
    {
        lblTf1.Text = $"{temps.GetValueOrDefault(0, 0):F1} °C";
        lblTf2.Text = $"{temps.GetValueOrDefault(1, 0):F1} °C";
        lblTs.Text = $"{temps.GetValueOrDefault(2, 0):F1} °C";
        lblTc.Text = $"{temps.GetValueOrDefault(3, 0):F1} °C";
        lblTCal.Text = $"{temps.GetValueOrDefault(16, 0):F1} °C";
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(() => OnStateChanged(sender, e));
            return;
        }
        UpdateButtonStates();
    }

    #endregion

    #region 按钮状态控制

    private void UpdateButtonStates()
    {
        var state = _testMaster.CurrentState;
        bool hasActiveTest = !string.IsNullOrEmpty(_testMaster.CurrentProductId);
        bool hasUnfinishedTest = false;

        // 检查是否有未保存的完成试验
        var unfinished = Core.AppGlobal.Instance.Db.CheckUnfinishedTest();
        hasUnfinishedTest = unfinished != null;

        btnNewTest.Enabled = (state == TestState.Idle && !hasUnfinishedTest) ||
                             (state == TestState.Preparing && !hasActiveTest && !hasUnfinishedTest);
        btnStartHeat.Enabled = state == TestState.Idle && hasActiveTest && !hasUnfinishedTest;
        btnStopHeat.Enabled = state == TestState.Preparing || state == TestState.Ready || state == TestState.Complete;
        btnStartRecord.Enabled = state == TestState.Ready;
        btnStopRecord.Enabled = state == TestState.Recording;
        btnTestRecord.Enabled = state == TestState.Complete;
    }

    #endregion

    #region 按钮事件处理

    private void BtnNewTest_Click(object? sender, EventArgs e)
    {
        var form = new NewTestForm();
        if (form.ShowDialog() == DialogResult.OK && form.TestCreated)
        {
            _testMaster.ResetToIdle();
            _daqWorker.SetCooling(false);
            _daqWorker.SetRecording(false);

            string productId = form.ProductId ?? "";
            string testId = form.TestId ?? "";
            string productName = form.ProductNameVal;
            string operatorName = form.GetOperatorName();
            double preWeight = form.GetPreWeight();
            double ambTemp = form.GetAmbTemp();
            double ambHumi = form.GetAmbHumi();
            var mode = form.GetDurationMode();
            int targetSeconds = form.GetTargetDuration();

            // 设置试验信息，但保持Idle状态
            _testMaster.SetCurrentTest(productId, testId, operatorName,
                                        preWeight, ambTemp, ambHumi, productName,
                                        mode, targetSeconds);

            AddMessage($"新试验已创建，样品：{productId}，请点击「开始升温」");
        }
    }

    private void BtnStartHeat_Click(object? sender, EventArgs e)
    {
        _testMaster.StartHeating();
        _daqWorker.SetCooling(false);
        _daqWorker.SetRecording(false);
    }

    private void BtnStopHeat_Click(object? sender, EventArgs e)
    {
        _testMaster.StopHeating();
        _daqWorker.SetCooling(true);
        _daqWorker.SetRecording(false);
    }

    private void BtnStartRecord_Click(object? sender, EventArgs e)
    {
        _testMaster.StartRecording();
        _daqWorker.SetRecording(true);
    }

    private void BtnStopRecord_Click(object? sender, EventArgs e)
    {
        _testMaster.StopRecording();
        _daqWorker.SetRecording(false);
    }

    private void BtnTestRecord_Click(object? sender, EventArgs e)
    {
        var form = new TestRecordForm();
        if (form.ShowDialog() == DialogResult.OK && form.Saved)
        {
            // 先捕获温度曲线图
            string? chartPath = CaptureChartAsPng(_testMaster.CurrentTestId!);
            SaveTestRecord(form, chartPath);
        }
    }

    /// <summary>
    /// 使用 OxyPlot PngExporter 将当前曲线图保存为 PNG，返回文件路径
    /// </summary>
    private string? CaptureChartAsPng(string testId)
    {
        try
        {
            string dir = Path.Combine(Config.AppConfig.OutputDirectory, testId);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{testId}_曲线.png");

            // OxyPlot.WindowsForms 2.1 的 PngExporter
            using (var stream = new FileStream(path, FileMode.Create))
            {
                var exporter = new PngExporter { Width = 800, Height = 400, Resolution = 96 };
                exporter.Export(plotModel, stream);
            }
            return path;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "温度曲线图导出失败");
            return null;
        }
    }

    private void SaveTestRecord(TestRecordForm form, string? chartPngPath = null)
    {
        double preWeight = _testMaster.PreWeight;
        double postWeight = form.PostWeight;
        double lostWeight = preWeight - postWeight;
        double lostPer = lostWeight / preWeight * 100;

        double finalTf1 = _testMaster.GetFinalTf1();
        double finalTf2 = _testMaster.GetFinalTf2();
        double finalTs = _testMaster.GetFinalTs();
        double finalTc = _testMaster.GetFinalTc();

        double startTf1 = _testMaster.GetRecordStartTf1();
        double startTf2 = _testMaster.GetRecordStartTf2();
        double startTs = _testMaster.GetRecordStartTs();
        double startTc = _testMaster.GetRecordStartTc();

        double deltaTf1 = finalTf1 - startTf1;
        double deltaTf2 = finalTf2 - startTf2;
        double deltaTs = finalTs - startTs;
        double deltaTc = finalTc - startTc;
        double deltaTf = deltaTs; // 样品温升取表面温升

        string phenoCode = form.HasFlame ? $"flame:{form.FlameTime}:{form.FlameDuration}" : "";
        int totalTime = _testMaster.ElapsedSeconds;

        // Step 1: 先更新数据库（核心数据）
        try
        {
            Core.AppGlobal.Instance.Db.UpdateTestResult(
                _testMaster.CurrentProductId!, _testMaster.CurrentTestId!,
                preWeight, postWeight, lostPer, deltaTf, deltaTf1, deltaTf2, deltaTs, deltaTc,
                totalTime, phenoCode, form.FlameTime, form.FlameDuration,
                _testMaster.MaxTf1, _testMaster.MaxTf2, _testMaster.MaxTs, _testMaster.MaxTc,
                _testMaster.MaxTf1Time, _testMaster.MaxTf2Time, _testMaster.MaxTsTime, _testMaster.MaxTcTime,
                finalTf1, finalTf2, finalTs, finalTc,
                totalTime, totalTime, totalTime, totalTime,
                form.Memo
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库保存失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Serilog.Log.Error(ex, "数据库保存试验记录失败");
            return; // DB 失败就不继续了
        }

        // Step 2: 导出文件（每个独立 try-catch，一个失败不影响其他的）
        var exportErrors = new List<string>();
        var exportOk = new List<string>();

        // 导出CSV
        try
        {
            _exportService.ExportCsv(_testMaster.RecordingData, _testMaster.CurrentProductId!, _testMaster.CurrentTestId!);
            exportOk.Add("CSV");
        }
        catch (Exception ex)
        {
            exportErrors.Add($"CSV: {ex.Message}");
            Serilog.Log.Error(ex, "CSV导出失败");
        }

        // 导出Excel 和 PDF
        try
        {
            var record = Core.AppGlobal.Instance.Db.GetTest(_testMaster.CurrentProductId!, _testMaster.CurrentTestId!);
            if (record != null)
            {
                try
                {
                    _exportService.ExportExcel(record, _testMaster.RecordingData);
                    exportOk.Add("Excel");
                }
                catch (Exception ex)
                {
                    exportErrors.Add($"Excel: {ex.Message}");
                    Serilog.Log.Error(ex, "Excel导出失败");
                }

                try
                {
                    _exportService.ExportPdf(record, _testMaster.RecordingData, chartPngPath);
                    exportOk.Add("PDF");
                }
                catch (Exception ex)
                {
                    exportErrors.Add($"PDF: {ex.Message}");
                    Serilog.Log.Error(ex, "PDF导出失败");
                }
            }
        }
        catch (Exception ex)
        {
            exportErrors.Add($"读取记录: {ex.Message}");
            Serilog.Log.Error(ex, "读取试验记录失败");
        }

        // Step 3: 显示结果
        AddMessage("试验记录已保存，报告已生成");

        // 重置状态
        _testMaster.SaveRecordsAndReset();
        _daqWorker.SetRecording(false);
        _daqWorker.SetCooling(false);

        if (exportErrors.Count == 0)
        {
            MessageBox.Show("试验记录保存成功！\nCSV/Excel/PDF 报告已生成。", "保存成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else if (exportOk.Count > 0)
        {
            MessageBox.Show(
                $"数据已保存，但以下报告生成失败：\n\n{string.Join("\n", exportErrors)}",
                "部分成功", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            MessageBox.Show(
                $"数据已保存，但所有报告生成失败：\n\n{string.Join("\n", exportErrors)}",
                "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddMessage(string msg)
    {
        if (rtbMessages.InvokeRequired)
        {
            rtbMessages.BeginInvoke(() => AddMessage(msg));
            return;
        }
        rtbMessages.SelectionStart = rtbMessages.TextLength;
        rtbMessages.SelectionColor = AccentBlue;
        rtbMessages.AppendText("● ");
        rtbMessages.SelectionColor = TextSecondary;
        rtbMessages.AppendText($"{DateTime.Now:HH:mm:ss}  ");
        rtbMessages.SelectionColor = TextPrimary;
        rtbMessages.AppendText($"{msg}\n");
        rtbMessages.ScrollToCaret();
    }

    #endregion

    #region 记录查询 Tab

    private void BuildQueryTab()
    {
        tabQuery.BackColor = BgMain;

        // 内容容器 — 固定宽度，水平居中
        int contentW = 1100;
        var container = new Panel
        {
            Width = contentW,
            Height = 740,
            BackColor = Color.Transparent
        };
        tabQuery.Resize += (s, e) =>
        {
            container.Left = Math.Max(0, (tabQuery.Width - contentW) / 2);
        };

        // ---- 顶部筛选栏 ----
        var filterBar = new Panel
        {
            Location = new Point(0, 12),
            Size = new Size(contentW, 52),
            BackColor = BgPanel
        };

        var lblFrom = new Label { Text = "开始日期:", Location = new Point(20, 18), AutoSize = true, Font = new Font("Microsoft YaHei", 9), ForeColor = TextSecondary };
        dtpFrom = new DateTimePicker { Location = new Point(104, 14), Size = new Size(185, 25), Value = DateTime.Now.AddMonths(-1) };

        var lblTo = new Label { Text = "结束日期:", Location = new Point(304, 18), AutoSize = true, Font = new Font("Microsoft YaHei", 9), ForeColor = TextSecondary };
        dtpTo = new DateTimePicker { Location = new Point(388, 14), Size = new Size(185, 25), Value = DateTime.Now };

        var lblProduct = new Label { Text = "样品编号:", Location = new Point(588, 18), AutoSize = true, Font = new Font("Microsoft YaHei", 9), ForeColor = TextSecondary };
        txtSearchProductId = new TextBox { Location = new Point(672, 14), Size = new Size(130, 25), Font = new Font("Microsoft YaHei", 9), BackColor = BgCard, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle };

        var lblOp = new Label { Text = "操作员:", Location = new Point(817, 18), AutoSize = true, Font = new Font("Microsoft YaHei", 9), ForeColor = TextSecondary };
        cmbFilterOperator = new ComboBox { Location = new Point(886, 14), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Microsoft YaHei", 9), BackColor = BgCard, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };

        filterBar.Controls.AddRange(new Control[] {
            lblFrom, dtpFrom, lblTo, dtpTo, lblProduct, txtSearchProductId,
            lblOp, cmbFilterOperator
        });

        // ---- 按钮（独立一行，居中） ----
        var btnPanel = new Panel
        {
            Location = new Point(0, 72),
            Size = new Size(contentW, 40),
            BackColor = Color.Transparent
        };

        btnSearch = new Button
        {
            Text = "🔍  查询",
            Location = new Point((contentW - 200) / 2, 4),
            Size = new Size(95, 32),
            BackColor = AccentBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        btnSearch.Click += BtnSearch_Click;
        btnSearch.MouseEnter += (s, e) => btnSearch.BackColor = Color.FromArgb(0, 140, 230);
        btnSearch.MouseLeave += (s, e) => btnSearch.BackColor = AccentBlue;

        btnExportQuery = new Button
        {
            Text = "📥  导出",
            Location = new Point((contentW - 200) / 2 + 105, 4),
            Size = new Size(95, 32),
            BackColor = AccentTeal,
            ForeColor = Color.FromArgb(10, 10, 26),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnExportQuery.FlatAppearance.BorderSize = 0;
        btnExportQuery.Click += BtnExportQuery_Click;
        btnExportQuery.MouseEnter += (s, e) => btnExportQuery.BackColor = Color.FromArgb(0, 210, 130);
        btnExportQuery.MouseLeave += (s, e) => btnExportQuery.BackColor = AccentTeal;

        btnPanel.Controls.AddRange(new Control[] { btnSearch, btnExportQuery });

        // ---- 数据表 ----
        dgvRecords = new DataGridView
        {
            Location = new Point(0, 118),
            Size = new Size(contentW, 600),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = BgMain,
            BorderStyle = BorderStyle.None,
            GridColor = BorderColor,
            EnableHeadersVisualStyles = false
        };

        // 表头样式
        dgvRecords.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = BgPanel,
            ForeColor = AccentTeal,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            SelectionBackColor = BgPanel,
            SelectionForeColor = AccentTeal
        };
        dgvRecords.ColumnHeadersHeight = 34;
        dgvRecords.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // 默认单元格样式
        dgvRecords.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = BgCard,
            ForeColor = TextPrimary,
            Font = new Font("Microsoft YaHei", 9),
            SelectionBackColor = Color.FromArgb(0, 100, 180),
            SelectionForeColor = Color.White,
            Padding = new Padding(4, 2, 4, 2)
        };

        // 交替行样式
        dgvRecords.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(24, 24, 48),
            ForeColor = TextPrimary,
            Font = new Font("Microsoft YaHei", 9),
            SelectionBackColor = Color.FromArgb(0, 100, 180),
            SelectionForeColor = Color.White
        };

        dgvRecords.RowTemplate.Height = 28;
        dgvRecords.RowHeadersVisible = false;
        dgvRecords.CellDoubleClick += DgvRecords_CellDoubleClick;

        container.Controls.AddRange(new Control[] { filterBar, btnPanel, dgvRecords });
        tabQuery.Controls.Add(container);

        // 初始居中
        container.Left = Math.Max(0, (tabQuery.Width - contentW) / 2);

        LoadOperatorList();
    }

    private void LoadOperatorList()
    {
        try
        {
            var ops = Core.AppGlobal.Instance.Db.GetDistinctOperators();
            cmbFilterOperator.Items.Clear();
            cmbFilterOperator.Items.Add("(全部)");
            foreach (var op in ops)
                cmbFilterOperator.Items.Add(op);
            cmbFilterOperator.SelectedIndex = 0;
        }
        catch { }
    }

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        try
        {
            string? productId = string.IsNullOrWhiteSpace(txtSearchProductId.Text) ? null : txtSearchProductId.Text.Trim();
            string? operatorName = cmbFilterOperator.SelectedIndex > 0 ? cmbFilterOperator.SelectedItem?.ToString() : null;

            var records = Core.AppGlobal.Instance.Db.QueryTests(
                dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1), productId, operatorName);

            dgvRecords.DataSource = records.Select(r => new
            {
                r.TestId,
                r.ProductId,
                试验日期 = r.TestDate.ToString("yyyy-MM-dd"),
                r.Operator,
                试验前质量 = r.PreWeight,
                试验后质量 = r.PostWeight,
                失重率 = $"{r.LostWeightPer:F2}%",
                温升 = $"{r.DeltaTf:F2}°C",
                时长 = $"{r.TotalTestTime}秒",
                火焰时长 = $"{r.FlameDuration}秒",
                已保存 = r.Flag == "10000000" ? "是" : "否"
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvRecords_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        try
        {
            string testId = dgvRecords.Rows[e.RowIndex].Cells["TestId"].Value?.ToString() ?? "";
            string productId = dgvRecords.Rows[e.RowIndex].Cells["ProductId"].Value?.ToString() ?? "";

            var record = Core.AppGlobal.Instance.Db.GetTest(productId, testId);
            if (record != null)
            {
                var detailForm = new TestDetailForm(record, _exportService);
                detailForm.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载详情失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnExportQuery_Click(object? sender, EventArgs e)
    {
        if (dgvRecords.Rows.Count == 0)
        {
            MessageBox.Show("没有可导出的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // 导出查询结果到Excel
            var records = Core.AppGlobal.Instance.Db.QueryTests(
                dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

            if (records.Count > 0)
            {
                // 使用第一条记录的格式导出
                _exportService.ExportExcel(records[0]);
                MessageBox.Show("查询结果已导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region 设备校准 Tab

    private void BuildCalibrationTab()
    {
        _calibrationForm = new CalibrationForm
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill,
            Visible = true
        };
        tabCalibration.Controls.Add(_calibrationForm);
    }

    #endregion

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // 初始化时加载一次查询数据
        BeginInvoke(() => BtnSearch_Click(this, EventArgs.Empty));
    }
}