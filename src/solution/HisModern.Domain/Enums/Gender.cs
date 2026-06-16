namespace HisModern.Domain.Enums;

/// <summary>
/// 病患性別。取代 legacy 以字串 "M"/"F"/"男"/"女" 混用造成的不一致。
/// </summary>
public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

public static class GenderExtensions
{
    /// <summary>
    /// 將 legacy 資料中混用的性別字串 ("M"/"F"/"男"/"女") 正規化為 <see cref="Gender"/>。
    /// </summary>
    public static Gender FromLegacyString(string? value) => value?.Trim() switch
    {
        "M" or "m" or "男" => Gender.Male,
        "F" or "f" or "女" => Gender.Female,
        _ => Gender.Unknown
    };

    /// <summary>輸出統一的顯示字串。</summary>
    public static string ToDisplayText(this Gender gender) => gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };
}
