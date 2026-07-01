namespace ISO11820.Models;

/// <summary>
/// 操作员/用户模型 — 对应 operators 表
/// </summary>
public class Operator
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Pwd { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;

    public bool IsAdmin => UserType == "admin";
}
