namespace ISO11820.UI.Controls;

/// <summary>
/// 5 通道温度显示面板 — 简洁风格
/// </summary>
public class TemperaturePanel : UserControl
{
    private readonly Label[] _vals = new Label[5];

    private static readonly (string Name, Color Color)[] Ch = new[]
    {
        ("炉温1",    Color.FromArgb(200, 40, 40)),
        ("炉温2",    Color.FromArgb(40, 110, 200)),
        ("表面温度",  Color.FromArgb(30, 140, 70)),
        ("中心温度",  Color.FromArgb(220, 130, 20)),
        ("校准温度",  Color.FromArgb(130, 90, 180)),
    };

    public TemperaturePanel()
    {
        this.BackColor = Color.White;
        this.Width = 240;
        this.Height = 300;
        this.Padding = new Padding(6);

        for (int i = 0; i < 5; i++)
        {
            int y = 2 + i * 58;

            var pnl = new Panel
            {
                Location = new Point(4, y),
                Size = new Size(230, 54),
                BackColor = Color.FromArgb(248, 249, 252),
                Cursor = Cursors.Default,
                Padding = new Padding(8)
            };

            var nameLbl = new Label
            {
                Text = Ch[i].Name,
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.Gray,
                Location = new Point(10, 3),
                Size = new Size(80, 14),
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(nameLbl);

            // 数值右对齐
            _vals[i] = new Label
            {
                Text = "0.0",
                Font = new Font("Consolas", 22, FontStyle.Bold),
                ForeColor = Ch[i].Color,
                Location = new Point(10, 16),
                Size = new Size(175, 32),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnl.Controls.Add(_vals[i]);

            var unit = new Label
            {
                Text = "°C",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(188, 18),
                Size = new Size(38, 28),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(unit);

            this.Controls.Add(pnl);
        }
    }

    public void UpdateValues(double tf1, double tf2, double ts, double tc, double tcal)
    {
        if (this.InvokeRequired) { this.Invoke(() => UpdateValues(tf1, tf2, ts, tc, tcal)); return; }
        var v = new[] { tf1, tf2, ts, tc, tcal };
        for (int i = 0; i < 5; i++) _vals[i].Text = v[i].ToString("F1");
    }
}
