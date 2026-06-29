using ISO11820.Core;
using ISO11820.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel.Tables;

namespace ISO11820.Services;

/// <summary>
/// PDFsharp 字体解析器 — 仅使用 .ttf 字体（PDFsharp 不支持 .ttc）
/// </summary>
public class WindowsFontResolver : IFontResolver
{
    private static readonly string FontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    // 预扫描结果：familyName → (regularFileName, boldFileName)
    private static readonly Dictionary<string, (string Regular, string? Bold)> _fonts
        = new(StringComparer.OrdinalIgnoreCase);

    static WindowsFontResolver()
    {
        // 只扫描 .ttf 格式（PDFsharp 不支持 .ttc TrueType Collection）
        ScanFont("Arial",         "arial.ttf",    "arialbd.ttf");
        ScanFont("SimHei",        "simhei.ttf",   null);           // 黑体 — 主要中文字体
        ScanFont("Times New Roman", "times.ttf",  "timesbd.ttf");
        ScanFont("Courier New",   "cour.ttf",     "courbd.ttf");
    }

    private static void ScanFont(string family, string regular, string? bold)
    {
        if (File.Exists(Path.Combine(FontsDir, regular)))
            _fonts[family] = (regular, bold != null && File.Exists(Path.Combine(FontsDir, bold)) ? bold : null);
    }

    public string DefaultFontName => "SimHei";

    public byte[] GetFont(string faceName)
    {
        string family = faceName;
        bool wantsBold = false;
        int pipe = faceName.IndexOf('|');
        if (pipe > 0)
        {
            family = faceName.Substring(0, pipe);
            wantsBold = faceName.Substring(pipe + 1) == "Bold";
        }

        // 查找字体文件
        string? fileName = null;
        if (_fonts.TryGetValue(family, out var f))
            fileName = (wantsBold && f.Bold != null) ? f.Bold : f.Regular;

        if (fileName == null)
        {
            // 回退：尝试直接作为文件名
            fileName = family;
            if (!fileName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                fileName += ".ttf";
        }

        string path = Path.Combine(FontsDir, fileName);
        if (!File.Exists(path))
        {
            // 回退：SimHei → Arial
            path = Path.Combine(FontsDir, _fonts.TryGetValue("SimHei", out var sh) ? sh.Regular : "arial.ttf");
        }

        return File.ReadAllBytes(path);
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!_fonts.ContainsKey(familyName))
        {
            // 检查是否作为文件名直接存在
            foreach (var ext in new[] { "", ".ttf" })
            {
                if (File.Exists(Path.Combine(FontsDir, familyName + ext)))
                {
                    _fonts[familyName] = (familyName + ext, null);
                    goto found;
                }
            }
            // 回退到 SimHei
            familyName = "SimHei";
        }
    found:

        var cached = _fonts[familyName];
        string faceName = familyName;
        bool needSimulateBold = isBold && cached.Bold == null;
        if (isBold && cached.Bold != null)
            faceName = familyName + "|Bold";

        return new FontResolverInfo(faceName,
            mustSimulateBold: needSimulateBold,
            mustSimulateItalic: isItalic);
    }
}

/// <summary>
/// 导出服务 — CSV / Excel / PDF
/// </summary>
public class ExportService
{
    private readonly Data.DbHelper _db;

    public ExportService(Data.DbHelper db)
    {
        _db = db;
    }

    // ==================== CSV 导出 ====================

    public string ExportCsv(List<TemperatureRecord> records, string productId, string testId)
    {
        string baseDir = Config.AppConfig.TestDataDirectory;
        string dir = Path.Combine(baseDir, productId, testId);
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, "sensor_data.csv");

        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        foreach (var r in records)
        {
            writer.WriteLine($"{r.Time},{r.Temp1:F1},{r.Temp2:F1},{r.TempSurface:F1},{r.TempCenter:F1},{r.TempCalibration:F1}");
        }

        return filePath;
    }

    // ==================== Excel 导出 ====================

    public string ExportExcel(TestMasterRecord record, List<TemperatureRecord>? tempData = null)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        string dir = Path.Combine(Config.AppConfig.OutputDirectory, record.TestId);
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, $"{record.TestId}_报告.xlsx");

        var data = tempData ?? new List<TemperatureRecord>();

        using var package = new ExcelPackage();

        // Sheet1: 试验信息
        var sheet1 = package.Workbook.Worksheets.Add("试验信息");
        WriteTestInfoSheet(sheet1, record);

        // Sheet2: 温度数据
        var sheet2 = package.Workbook.Worksheets.Add("温度数据");
        WriteTemperatureDataSheet(sheet2, data);

        // Sheet3: 温度曲线图
        if (data.Count > 0)
        {
            var sheet3 = package.Workbook.Worksheets.Add("温度曲线");
            WriteTemperatureChartSheet(sheet3, data);
        }

        package.SaveAs(new FileInfo(filePath));
        return filePath;
    }

    private void WriteTestInfoSheet(ExcelWorksheet sheet, TestMasterRecord record)
    {
        sheet.Cells["A1"].Value = "ISO 11820 不燃性试验报告";
        sheet.Cells["A1"].Style.Font.Size = 16;
        sheet.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        WriteInfoRow(sheet, ref row, "样品编号", record.ProductId);
        WriteInfoRow(sheet, ref row, "试验ID", record.TestId);
        WriteInfoRow(sheet, ref row, "试验日期", record.TestDate.ToString("yyyy-MM-dd"));
        WriteInfoRow(sheet, ref row, "操作员", record.Operator);
        WriteInfoRow(sheet, ref row, "环境温度", $"{record.AmbTemp:F1} °C");
        WriteInfoRow(sheet, ref row, "环境湿度", $"{record.AmbHumi:F1} %");
        WriteInfoRow(sheet, ref row, "设备编号", record.ApparatusId);
        WriteInfoRow(sheet, ref row, "设备名称", record.ApparatusName);
        WriteInfoRow(sheet, ref row, "试验前质量", $"{record.PreWeight:F2} g");
        WriteInfoRow(sheet, ref row, "试验后质量", $"{record.PostWeight:F2} g");
        WriteInfoRow(sheet, ref row, "失重量", $"{record.LostWeight:F2} g");
        WriteInfoRow(sheet, ref row, "失重率", $"{record.LostWeightPer:F2} %");
        WriteInfoRow(sheet, ref row, "样品温升(ΔT)", $"{record.DeltaTf:F2} °C");
        WriteInfoRow(sheet, ref row, "炉温1温升", $"{record.DeltaTf1:F2} °C");
        WriteInfoRow(sheet, ref row, "炉温2温升", $"{record.DeltaTf2:F2} °C");
        WriteInfoRow(sheet, ref row, "表面温升", $"{record.DeltaTs:F2} °C");
        WriteInfoRow(sheet, ref row, "中心温升", $"{record.DeltaTc:F2} °C");
        WriteInfoRow(sheet, ref row, "试验时长", $"{record.TotalTestTime} 秒");
        WriteInfoRow(sheet, ref row, "火焰持续时间", $"{record.FlameDuration} 秒");

        // 判定结论
        row++;
        bool passed = record.DeltaTf <= 50 && record.LostWeightPer <= 50 && record.FlameDuration < 5;
        sheet.Cells[$"A{row}"].Value = "判定结论";
        sheet.Cells[$"B{row}"].Value = passed ? "通过" : "不通过";
        sheet.Cells[$"B{row}"].Style.Font.Color.SetColor(passed ? System.Drawing.Color.Green : System.Drawing.Color.Red);
        sheet.Cells[$"B{row}"].Style.Font.Bold = true;

        sheet.Column(1).Width = 20;
        sheet.Column(2).Width = 30;
    }

    private void WriteInfoRow(ExcelWorksheet sheet, ref int row, string label, string value)
    {
        sheet.Cells[$"A{row}"].Value = label;
        sheet.Cells[$"B{row}"].Value = value;
        row++;
    }

    private void WriteTemperatureDataSheet(ExcelWorksheet sheet, List<TemperatureRecord> data)
    {
        sheet.Cells["A1"].Value = "时间(秒)";
        sheet.Cells["B1"].Value = "炉温1(°C)";
        sheet.Cells["C1"].Value = "炉温2(°C)";
        sheet.Cells["D1"].Value = "表面温度(°C)";
        sheet.Cells["E1"].Value = "中心温度(°C)";
        sheet.Cells["F1"].Value = "校准温度(°C)";

        for (int i = 0; i < data.Count; i++)
        {
            int row = i + 2;
            sheet.Cells[$"A{row}"].Value = data[i].Time;
            sheet.Cells[$"B{row}"].Value = data[i].Temp1;
            sheet.Cells[$"C{row}"].Value = data[i].Temp2;
            sheet.Cells[$"D{row}"].Value = data[i].TempSurface;
            sheet.Cells[$"E{row}"].Value = data[i].TempCenter;
            sheet.Cells[$"F{row}"].Value = data[i].TempCalibration;
        }

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 15;
        sheet.Column(3).Width = 15;
        sheet.Column(4).Width = 15;
        sheet.Column(5).Width = 15;
        sheet.Column(6).Width = 15;
    }

    private void WriteTemperatureChartSheet(ExcelWorksheet sheet, List<TemperatureRecord> data)
    {
        int dataStartRow = 2;
        sheet.Cells[$"A{dataStartRow}"].Value = "Time";
        sheet.Cells[$"B{dataStartRow}"].Value = "TF1";
        sheet.Cells[$"C{dataStartRow}"].Value = "TF2";
        sheet.Cells[$"D{dataStartRow}"].Value = "TS";
        sheet.Cells[$"E{dataStartRow}"].Value = "TC";

        for (int i = 0; i < data.Count; i++)
        {
            int row = dataStartRow + 1 + i;
            sheet.Cells[$"A{row}"].Value = data[i].Time;
            sheet.Cells[$"B{row}"].Value = data[i].Temp1;
            sheet.Cells[$"C{row}"].Value = data[i].Temp2;
            sheet.Cells[$"D{row}"].Value = data[i].TempSurface;
            sheet.Cells[$"E{row}"].Value = data[i].TempCenter;
        }

        int dataEndRow = dataStartRow + data.Count;

        var chart = sheet.Drawings.AddChart("TemperatureChart", eChartType.Line);
        chart.Title.Text = "温度曲线";
        chart.XAxis.Title.Text = "时间 (秒)";
        chart.YAxis.Title.Text = "温度 (°C)";
        chart.YAxis.MaxValue = 800;
        chart.YAxis.MinValue = 0;

        var tf1Series = chart.Series.Add(sheet.Cells[$"B{dataStartRow + 1}:B{dataEndRow}"], sheet.Cells[$"A{dataStartRow + 1}:A{dataEndRow}"]);
        tf1Series.Header = "炉温1";

        var tf2Series = chart.Series.Add(sheet.Cells[$"C{dataStartRow + 1}:C{dataEndRow}"], sheet.Cells[$"A{dataStartRow + 1}:A{dataEndRow}"]);
        tf2Series.Header = "炉温2";

        var tsSeries = chart.Series.Add(sheet.Cells[$"D{dataStartRow + 1}:D{dataEndRow}"], sheet.Cells[$"A{dataStartRow + 1}:A{dataEndRow}"]);
        tsSeries.Header = "表面温度";

        var tcSeries = chart.Series.Add(sheet.Cells[$"E{dataStartRow + 1}:E{dataEndRow}"], sheet.Cells[$"A{dataStartRow + 1}:A{dataEndRow}"]);
        tcSeries.Header = "中心温度";

        chart.SetSize(800, 500);
        chart.SetPosition(0, 0, 7, 0);
    }

    // ==================== PDF 导出 ====================

    private static bool _fontResolverRegistered = false;
    private static readonly object _fontLock = new();

    /// <summary>
    /// 导出 PDF 报告，包含：试验概要 + 温度曲线图 + 判定结论
    /// </summary>
    /// <param name="chartPngPath">温度曲线图 PNG 文件路径（可选）</param>
    public string ExportPdf(TestMasterRecord record, List<TemperatureRecord>? tempData = null,
                            string? chartPngPath = null)
    {
        if (!Config.AppConfig.EnablePdfExport)
            return "";

        // 注册字体解析器（仅一次）
        if (!_fontResolverRegistered)
        {
            lock (_fontLock)
            {
                if (!_fontResolverRegistered)
                {
                    GlobalFontSettings.FontResolver = new WindowsFontResolver();
                    _fontResolverRegistered = true;
                }
            }
        }

        string dir = Path.Combine(Config.AppConfig.OutputDirectory, record.TestId);
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, $"{record.TestId}_报告.pdf");

        var document = new MigraDoc.DocumentObjectModel.Document();
        document.Info.Title = "ISO 11820 不燃性试验报告";
        document.Info.Author = record.Operator;

        // 设置默认字体为黑体（SimHei = .ttf 格式，PDFsharp 支持，中文不乱码）
        var normalStyle = document.Styles["Normal"];
        normalStyle.Font.Name = "SimHei";
        normalStyle.Font.Size = 10;
        normalStyle.ParagraphFormat.SpaceAfter = "3pt";

        var section = document.AddSection();
        section.PageSetup.TopMargin = "2cm";
        section.PageSetup.BottomMargin = "2cm";

        // ========== 1. 标题 ==========
        var title = section.AddParagraph("ISO 11820 不燃性试验报告");
        title.Format.Font.Name = "SimHei";
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.SpaceAfter = "0.8cm";

        // ========== 2. 试验概要表 ==========
        var heading1 = section.AddParagraph("一、试验概要");
        heading1.Format.Font.Name = "SimHei";
        heading1.Format.Font.Size = 13;
        heading1.Format.Font.Bold = true;
        heading1.Format.SpaceAfter = "0.3cm";

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.AddColumn("5cm");
        table.AddColumn("10cm");

        AddTableRow(table, "样品编号", record.ProductId);
        AddTableRow(table, "试验ID", record.TestId);
        AddTableRow(table, "试验日期", record.TestDate.ToString("yyyy-MM-dd"));
        AddTableRow(table, "操作员", record.Operator);
        AddTableRow(table, "环境温度", $"{record.AmbTemp:F1} °C");
        AddTableRow(table, "环境湿度", $"{record.AmbHumi:F1} %");
        AddTableRow(table, "设备", record.ApparatusName);
        AddTableRow(table, "试验前质量", $"{record.PreWeight:F2} g");
        AddTableRow(table, "试验后质量", $"{record.PostWeight:F2} g");
        AddTableRow(table, "失重量", $"{record.LostWeight:F2} g");
        AddTableRow(table, "失重率", $"{record.LostWeightPer:F2} %");
        AddTableRow(table, "样品温升(ΔT)", $"{record.DeltaTf:F2} °C");
        AddTableRow(table, "炉温1温升", $"{record.DeltaTf1:F2} °C");
        AddTableRow(table, "炉温2温升", $"{record.DeltaTf2:F2} °C");
        AddTableRow(table, "表面温升", $"{record.DeltaTs:F2} °C");
        AddTableRow(table, "中心温升", $"{record.DeltaTc:F2} °C");
        AddTableRow(table, "试验时长", $"{record.TotalTestTime} 秒");
        AddTableRow(table, "火焰持续时间", $"{record.FlameDuration} 秒");

        if (!string.IsNullOrEmpty(record.Memo))
        {
            AddTableRow(table, "备注", record.Memo);
        }

        // ========== 3. 温度曲线图 ==========
        if (!string.IsNullOrEmpty(chartPngPath) && File.Exists(chartPngPath))
        {
            section.AddParagraph(); // 空行
            var heading2 = section.AddParagraph("二、温度曲线");
            heading2.Format.Font.Name = "SimHei";
            heading2.Format.Font.Size = 13;
            heading2.Format.Font.Bold = true;
            heading2.Format.SpaceAfter = "0.3cm";

            var image = section.AddImage(chartPngPath);
            image.Width = "15cm";
            // 保持宽高比
            image.LockAspectRatio = true;
        }

        // ========== 4. 判定结论 ==========
        section.AddParagraph(); // 空行
        var heading3 = section.AddParagraph("三、判定结论");
        heading3.Format.Font.Name = "SimHei";
        heading3.Format.Font.Size = 13;
        heading3.Format.Font.Bold = true;
        heading3.Format.SpaceAfter = "0.3cm";

        bool passed = record.DeltaTf <= 50 && record.LostWeightPer <= 50 && record.FlameDuration < 5;
        var conclusion = section.AddParagraph();
        conclusion.AddText(passed ? "结论：通过" : "结论：不通过");
        conclusion.Format.Font.Name = "SimHei";
        conclusion.Format.Font.Size = 14;
        conclusion.Format.Font.Bold = true;
        conclusion.Format.Font.Color = passed
            ? MigraDoc.DocumentObjectModel.Color.FromRgb(0, 128, 0)
            : MigraDoc.DocumentObjectModel.Color.FromRgb(255, 0, 0);

        // 判定标准说明
        section.AddParagraph();
        var criteria = section.AddParagraph();
        criteria.AddText("判定标准：ΔT ≤ 50°C | 失重率 ≤ 50% | 火焰持续时间 < 5s");
        criteria.Format.Font.Name = "SimHei";
        criteria.Format.Font.Size = 9;
        criteria.Format.Font.Color = MigraDoc.DocumentObjectModel.Color.FromRgb(128, 128, 128);

        // 渲染
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(filePath);

        return filePath;
    }

    private void AddTableRow(MigraDoc.DocumentObjectModel.Tables.Table table, string label, string value)
    {
        var row = table.AddRow();
        var cell0 = row.Cells[0];
        var p0 = cell0.AddParagraph(label);
        p0.Format.Font.Name = "SimHei";
        p0.Format.Font.Bold = true;

        var cell1 = row.Cells[1];
        var p1 = cell1.AddParagraph(value);
        p1.Format.Font.Name = "SimHei";
    }
}
