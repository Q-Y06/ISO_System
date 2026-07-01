using ISO11820.Global;
using ISO11820.Models;

namespace ISO11820.UI.Forms;

/// <summary>
/// 校准详情窗口 — 显示9点温度表格和均匀性指标
/// </summary>
public partial class CalibrationForm : Form
{
    private readonly CalibrationRecord _record;

    public CalibrationForm(CalibrationRecord record)
    {
        _record = record;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = $"校准详情 - {_record.Id[..8]}";
        this.Size = new Size(550, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.BackColor = Color.FromArgb(45, 45, 45);
        this.ForeColor = Color.White;

        var rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 35),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        };
        this.Controls.Add(rtb);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("════════════════════════════════");
        sb.AppendLine("  设备校准详情");
        sb.AppendLine("════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"校准ID:   {_record.Id}");
        sb.AppendLine($"日期:     {_record.CalibrationDate}");
        sb.AppendLine($"类型:     {_record.CalibrationType}");
        sb.AppendLine($"操作员:   {_record.Operator}");
        sb.AppendLine($"设备ID:   {_record.ApparatusId}");
        sb.AppendLine();
        sb.AppendLine("--- 炉壁9点温度 (°C) ---");
        sb.AppendLine();
        sb.AppendLine("        轴1      轴2      轴3      层平均");
        sb.AppendLine($"A层:   {_record.TempA1,6:F1}  {_record.TempA2,6:F1}  {_record.TempA3,6:F1}  {_record.TAvgLevela,8:F1}");
        sb.AppendLine($"B层:   {_record.TempB1,6:F1}  {_record.TempB2,6:F1}  {_record.TempB3,6:F1}  {_record.TAvgLevelb,8:F1}");
        sb.AppendLine($"C层:   {_record.TempC1,6:F1}  {_record.TempC2,6:F1}  {_record.TempC3,6:F1}  {_record.TAvgLevelc,8:F1}");
        sb.AppendLine("轴平均:");
        sb.AppendLine($"      {_record.TAvgAxis1,6:F1}  {_record.TAvgAxis2,6:F1}  {_record.TAvgAxis3,6:F1}");
        sb.AppendLine();
        sb.AppendLine("--- 均匀性指标 ---");
        sb.AppendLine($"总均温:         {_record.TAvg:F1} °C");
        sb.AppendLine($"最大偏差:       {_record.MaxDeviation:F2} °C");
        sb.AppendLine($"均匀性结果:     {_record.UniformityResult:F2} °C");
        sb.AppendLine($"平均轴偏差:     {_record.TAvgDevAxis:F2} °C");
        sb.AppendLine($"平均层偏差:     {_record.TAvgDevLevel:F2} °C");
        sb.AppendLine();
        sb.AppendLine("--- 各轴偏差 ---");
        sb.AppendLine($"轴1: {_record.TDevAxis1,8:F2} °C");
        sb.AppendLine($"轴2: {_record.TDevAxis2,8:F2} °C");
        sb.AppendLine($"轴3: {_record.TDevAxis3,8:F2} °C");
        sb.AppendLine();
        sb.AppendLine("--- 各层偏差 ---");
        sb.AppendLine($"A层: {_record.TDevLevela,8:F2} °C");
        sb.AppendLine($"B层: {_record.TDevLevelb,8:F2} °C");
        sb.AppendLine($"C层: {_record.TDevLevelc,8:F2} °C");
        sb.AppendLine();
        sb.AppendLine($"判定结果: {( _record.PassedCriteria == 1 ? "✓ 通过" : "✗ 未通过")}");
        sb.AppendLine($"备注:     {_record.Remarks}");

        rtb.Text = sb.ToString();
    }
}
