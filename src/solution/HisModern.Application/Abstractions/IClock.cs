namespace HisModern.Application.Abstractions;

/// <summary>
/// 時間抽象。取代 legacy 直接散落呼叫 <c>DateTime.Now</c>，
/// 讓「今天」可被注入以利測試 (年齡、報表、門診日期皆依賴它)。
/// </summary>
public interface IClock
{
    /// <summary>目前日期 (本地)。</summary>
    DateOnly Today { get; }

    /// <summary>目前日期時間 (本地)。</summary>
    DateTime Now { get; }
}
