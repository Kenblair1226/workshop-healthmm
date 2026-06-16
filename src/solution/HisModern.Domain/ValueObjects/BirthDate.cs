using System.Globalization;

namespace HisModern.Domain.ValueObjects;

/// <summary>
/// 生日值物件。集中處理 legacy 散落在三處、重複實作的年齡計算邏輯，
/// 並支援 legacy 兩種日期格式 ("yyyy/MM/dd" 與 "yyyy-MM-dd")。
/// </summary>
public readonly record struct BirthDate
{
    /// <summary>支援 legacy 出現過的日期格式（已先將 '-' 正規化為 '/'）。</summary>
    private static readonly string[] AcceptedFormats =
    {
        "yyyy/MM/dd", "yyyy/M/d", "yyyy/MM/d", "yyyy/M/dd"
    };

    public DateOnly Value { get; }

    public BirthDate(DateOnly value) => Value = value;

    /// <summary>
    /// 解析 legacy 生日字串。可接受 "yyyy/MM/dd" 或 "yyyy-MM-dd"。
    /// </summary>
    public static BirthDate Parse(string? raw)
    {
        if (!TryParse(raw, out var birthDate))
        {
            throw new FormatException($"無法解析生日字串: '{raw}'");
        }

        return birthDate;
    }

    /// <summary>
    /// 嘗試解析 legacy 生日字串，失敗時回傳 false（不丟例外，避免 legacy 的吞例外行為）。
    /// </summary>
    public static bool TryParse(string? raw, out BirthDate birthDate)
    {
        birthDate = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // legacy 偶爾出現 "yyyy-MM-dd"，統一正規化為 '/' 後再解析。
        var normalized = raw.Trim().Replace('-', '/');

        if (DateOnly.TryParseExact(normalized, AcceptedFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            birthDate = new BirthDate(parsed);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 計算到指定日期為止的足歲年齡（生日當天即算新的一歲）。
    /// 唯一一處年齡計算邏輯，取代 legacy 中重複三次的程式碼。
    /// </summary>
    public int AgeAsOf(DateOnly asOf)
    {
        var age = asOf.Year - Value.Year;

        // 今年生日還沒到，減一歲。
        if (asOf < Value.AddYears(age))
        {
            age--;
        }

        return age;
    }

    public override string ToString() => Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
}
