namespace HisModern.Domain.Enums;

/// <summary>
/// 掛號狀態。取代 legacy 用 magic number (0/1/2/3) 表示狀態。
/// 數值刻意對齊 legacy 以維持對外 JSON 契約 (status 欄位)。
/// </summary>
public enum RegistrationStatus
{
    Registered = 0, // 已掛號
    CheckedIn = 1,  // 已報到
    Completed = 2,  // 看診完成
    Cancelled = 3   // 已取消
}

public static class RegistrationStatusExtensions
{
    public static string ToDisplayText(this RegistrationStatus status) => status switch
    {
        RegistrationStatus.Registered => "已掛號",
        RegistrationStatus.CheckedIn => "已報到",
        RegistrationStatus.Completed => "看診完成",
        RegistrationStatus.Cancelled => "已取消",
        _ => "未知"
    };
}
