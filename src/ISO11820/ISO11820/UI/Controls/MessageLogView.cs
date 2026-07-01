namespace ISO11820.UI.Controls;

/// <summary>
/// 系统消息日志控件 — 基于 RichTextBox
/// </summary>
public class MessageLogView : UserControl
{
    private readonly RichTextBox _rtb;

    public MessageLogView()
    {
        this.Width = 500;
        this.Height = 150;

        _rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.White,
            Font = new Font("Consolas", 9f),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            WordWrap = true
        };
        this.Controls.Add(_rtb);

        // 标题标签
        var titleLabel = new Label
        {
            Text = " 系统消息",
            Dock = DockStyle.Top,
            Height = 20,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        this.Controls.Add(titleLabel);
        // 把 RichTextBox 重新置底
        _rtb.BringToFront();
    }

    /// <summary>
    /// 追加一条消息（线程安全）
    /// </summary>
    public void Append(string time, string message, Color? color = null)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(() => Append(time, message, color));
            return;
        }

        var msgColor = color ?? Color.White;
        _rtb.SelectionStart = _rtb.TextLength;
        _rtb.SelectionLength = 0;
        _rtb.SelectionColor = Color.Gray;
        _rtb.AppendText($"{time}  ");
        _rtb.SelectionColor = msgColor;
        _rtb.AppendText($"{message}\n");

        // 自动滚动到底部
        _rtb.SelectionStart = _rtb.TextLength;
        _rtb.ScrollToCaret();

        // 限制最大行数
        if (_rtb.Lines.Length > 500)
        {
            var lines = _rtb.Lines;
            _rtb.Text = string.Join("\n", lines.Skip(lines.Length - 300));
        }
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    public void Clear()
    {
        if (this.InvokeRequired)
        {
            this.Invoke(() => Clear());
            return;
        }
        _rtb.Clear();
    }
}
