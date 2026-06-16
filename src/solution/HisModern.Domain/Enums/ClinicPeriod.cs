namespace HisModern.Domain.Enums;

/// <summary>
/// 門診診次。取代 legacy 用 magic number (1/2/3) 表示診次。
/// </summary>
public enum ClinicPeriod
{
    Morning = 1,   // 早診
    Afternoon = 2, // 午診
    Night = 3      // 晚診
}

public static class ClinicPeriodExtensions
{
    public static string ToDisplayText(this ClinicPeriod period) => period switch
    {
        ClinicPeriod.Morning => "早診",
        ClinicPeriod.Afternoon => "午診",
        ClinicPeriod.Night => "晚診",
        _ => "未知"
    };
}
