using HisModern.Application.Abstractions;

namespace HisModern.Tests.Common;

/// <summary>
/// 可控制「現在時間」的測試用時鐘，讓年齡 / 報表 / 門診日期相關測試具確定性。
/// </summary>
public sealed class FixedClock : IClock
{
    public FixedClock(DateOnly today)
    {
        Today = today;
        Now = today.ToDateTime(new TimeOnly(9, 0));
    }

    public DateOnly Today { get; }
    public DateTime Now { get; }
}
