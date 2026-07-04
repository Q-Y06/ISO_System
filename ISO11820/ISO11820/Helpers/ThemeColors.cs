namespace ISO11820.Helpers;

/// <summary>统一暗色主题配色</summary>
public static class ThemeColors
{
    // 背景
    public static Color BgDark { get; } = Color.FromArgb(30, 30, 30);
    public static Color BgMedium { get; } = Color.FromArgb(45, 45, 45);
    public static Color BgLight { get; } = Color.FromArgb(40, 40, 40);
    public static Color BgInput { get; } = Color.FromArgb(50, 50, 50);
    public static Color BgLog { get; } = Color.FromArgb(15, 15, 15);
    public static Color BgLed { get; } = Color.FromArgb(20, 20, 20);
    public static Color BgLedValue { get; } = Color.FromArgb(10, 10, 10);

    // 文字
    public static Color TextPrimary { get; } = Color.White;
    public static Color TextSecondary { get; } = Color.FromArgb(200, 200, 200);
    public static Color TextMuted { get; } = Color.FromArgb(180, 180, 180);
    public static Color TextGray { get; } = Color.FromArgb(100, 100, 100);

    // 按钮
    public static Color AccentBlue { get; } = Color.FromArgb(60, 120, 200);
    public static Color AccentGreen { get; } = Color.FromArgb(40, 160, 80);
    public static Color AccentRed { get; } = Color.FromArgb(200, 60, 60);
    public static Color AccentPurple { get; } = Color.FromArgb(140, 100, 180);
    public static Color AccentOrange { get; } = Color.FromArgb(200, 80, 60);
    public static Color AccentYellow { get; } = Color.FromArgb(180, 120, 60);
    public static Color AccentDarkGray { get; } = Color.FromArgb(120, 120, 120);

    // 表格
    public static Color GridBg { get; } = Color.FromArgb(35, 35, 35);
    public static Color GridAltBg { get; } = Color.FromArgb(42, 42, 42);
    public static Color GridHeader { get; } = Color.FromArgb(50, 50, 50);
    public static Color GridBorder { get; } = Color.FromArgb(60, 60, 60);
    public static Color GridText { get; } = Color.FromArgb(220, 220, 220);
    public static Color GridSelBg { get; } = Color.FromArgb(0, 100, 180);

    // 状态
    public static Color StatusCyan { get; } = Color.FromArgb(0, 200, 200);
    public static Color CalGold { get; } = Color.FromArgb(200, 180, 100);

    // 温度曲线颜色
    public static Color TempFurnace1 { get; } = Color.FromArgb(255, 80, 80);
    public static Color TempFurnace2 { get; } = Color.FromArgb(255, 160, 60);
    public static Color TempSurface { get; } = Color.FromArgb(80, 200, 80);
    public static Color TempCenter { get; } = Color.FromArgb(80, 180, 255);
    public static Color TempCal { get; } = Color.FromArgb(200, 180, 100);

    // LoginForm 专用
    public static Color LoginBlue { get; } = Color.FromArgb(0, 122, 204);
    public static Color LoginGray { get; } = Color.FromArgb(80, 80, 80);
}