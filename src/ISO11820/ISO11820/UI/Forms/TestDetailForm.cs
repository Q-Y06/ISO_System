using ISO11820.Global;
using AppContext = ISO11820.Global.AppContext;

namespace ISO11820.UI.Forms;

/// <summary>
/// 试验详情查看窗口（只读）
/// </summary>
public partial class TestDetailForm : Form
{
    private readonly string _productId;
    private readonly string _testId;
    private readonly AppContext _ctx;

    public TestDetailForm(string productId, string testId)
    {
        _productId = productId;
        _testId = testId;
        _ctx = AppContext.Instance;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = $"试验详情 - {_testId}";
        this.Size = new Size(600, 650);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
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

        // 加载数据到 RichTextBox
        try
        {
            var test = _ctx.Db.GetTest(_productId, _testId);
            if (test != null)
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine("═══════════════════════════════════════");
                info.AppendLine("  ISO 11820 试验记录详情");
                info.AppendLine("═══════════════════════════════════════");
                info.AppendLine();
                info.AppendLine("【基本信息】");
                info.AppendLine($"  样品编号:     {test.ProductId}");
                info.AppendLine($"  试验ID:       {test.TestId}");
                info.AppendLine($"  试验日期:     {test.TestDate:yyyy-MM-dd}");
                info.AppendLine($"  操作员:       {test.Operator}");
                info.AppendLine($"  设备:         {test.ApparatusName} ({test.ApparatusId})");
                info.AppendLine($"  环境温度:     {test.AmbTemp:F1} °C");
                info.AppendLine($"  环境湿度:     {test.AmbHumi:F1} %");
                info.AppendLine($"  试验依据:     {test.According}");
                info.AppendLine();
                info.AppendLine("【质量数据】");
                info.AppendLine($"  试验前质量:   {test.PreWeight:F2} g");
                info.AppendLine($"  试验后质量:   {test.PostWeight:F2} g");
                info.AppendLine($"  失重量:       {test.LostWeight:F2} g");
                info.AppendLine($"  失重率:       {test.LostWeightPer:F2} %");
                info.AppendLine();
                info.AppendLine("【温度最大值】");
                info.AppendLine($"  炉温1:        {test.MaxTf1:F1} °C (第 {test.MaxTf1Time} 秒)");
                info.AppendLine($"  炉温2:        {test.MaxTf2:F1} °C (第 {test.MaxTf2Time} 秒)");
                info.AppendLine($"  表面温度:     {test.MaxTs:F1} °C (第 {test.MaxTsTime} 秒)");
                info.AppendLine($"  中心温度:     {test.MaxTc:F1} °C (第 {test.MaxTcTime} 秒)");
                info.AppendLine();
                info.AppendLine("【温度最终值】");
                info.AppendLine($"  炉温1:        {test.FinalTf1:F1} °C");
                info.AppendLine($"  炉温2:        {test.FinalTf2:F1} °C");
                info.AppendLine($"  表面温度:     {test.FinalTs:F1} °C");
                info.AppendLine($"  中心温度:     {test.FinalTc:F1} °C");
                info.AppendLine();
                info.AppendLine("【温升】");
                info.AppendLine($"  炉温1 温升:   {test.DeltaTf1:F2} °C");
                info.AppendLine($"  炉温2 温升:   {test.DeltaTf2:F2} °C");
                info.AppendLine($"  表面温升:     {test.DeltaTs:F2} °C");
                info.AppendLine($"  中心温升:     {test.DeltaTc:F2} °C");
                info.AppendLine($"  综合温升:     {test.DeltaTf:F2} °C (取表面温升)");
                info.AppendLine();
                info.AppendLine("【试验过程】");
                info.AppendLine($"  总时长:       {test.TotalTestTime} 秒");
                info.AppendLine($"  恒功率值:     {test.ConstPower}");
                info.AppendLine($"  现象:         {test.PhenoCode}");
                info.AppendLine($"  火焰时刻:     {test.FlameTime} 秒");
                info.AppendLine($"  火焰持续:     {test.FlameDuration} 秒");
                info.AppendLine($"  备注:         {test.Memo ?? "(无)"}");
                info.AppendLine($"  状态:         {(test.Flag == "10000000" ? "已完成" : "未保存")}");
                info.AppendLine();
                info.AppendLine("═══════════════════════════════════════");

                // 判定
                bool passed = test.DeltaTf <= 50 && test.LostWeightPer <= 50 && test.FlameDuration < 5;
                info.AppendLine($"  综合判定:     {(passed ? "✓ 通过" : "✗ 不通过")}");

                rtb.Text = info.ToString();
            }
            else
            {
                rtb.Text = "未找到试验记录";
            }
        }
        catch (Exception ex)
        {
            rtb.Text = $"加载失败: {ex.Message}";
        }
    }
}
