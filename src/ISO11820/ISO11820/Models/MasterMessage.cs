namespace ISO11820.Models;

/// <summary>
/// 系统消息数据结构
/// </summary>
public class MasterMessage
{
    public string Time { get; set; } = string.Empty;     // 格式 HH:mm:ss
    public string Message { get; set; } = string.Empty;
}
