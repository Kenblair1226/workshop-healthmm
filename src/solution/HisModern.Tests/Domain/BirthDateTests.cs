using HisModern.Domain.ValueObjects;

namespace HisModern.Tests.Domain;

/// <summary>
/// 商業規則 1：年齡 = 出生到指定日期的足歲；生日字串可為 "yyyy/MM/dd" 或 "yyyy-MM-dd"。
/// </summary>
public sealed class BirthDateTests
{
    [Theory]
    [InlineData("1985/03/12", 1985, 3, 12)]
    [InlineData("1990-07-08", 1990, 7, 8)]   // legacy 偶爾出現的 '-' 分隔
    [InlineData("2020/01/20", 2020, 1, 20)]
    [InlineData(" 2000/12/31 ", 2000, 12, 31)] // 容忍前後空白
    public void TryParse_AcceptsBothSeparators(string raw, int year, int month, int day)
    {
        var ok = BirthDate.TryParse(raw, out var birthDate);

        Assert.True(ok);
        Assert.Equal(new DateOnly(year, month, day), birthDate.Value);
    }

    [Fact]
    public void TryParse_BothSeparators_YieldSameDate()
    {
        Assert.True(BirthDate.TryParse("1990/07/08", out var slash));
        Assert.True(BirthDate.TryParse("1990-07-08", out var dash));

        Assert.Equal(slash.Value, dash.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-date")]
    [InlineData("2020/13/40")]
    public void TryParse_ReturnsFalse_ForInvalidInput(string? raw)
    {
        var ok = BirthDate.TryParse(raw, out _);

        Assert.False(ok);
    }

    [Fact]
    public void Parse_Throws_ForInvalidInput()
        => Assert.Throws<FormatException>(() => BirthDate.Parse("xx"));

    [Fact]
    public void AgeAsOf_OnBirthday_CountsTheNewYear()
    {
        var birth = new BirthDate(new DateOnly(2000, 6, 15));

        // 生日當天即算滿歲。
        Assert.Equal(26, birth.AgeAsOf(new DateOnly(2026, 6, 15)));
    }

    [Fact]
    public void AgeAsOf_DayBeforeBirthday_IsOneLess()
    {
        var birth = new BirthDate(new DateOnly(2000, 6, 15));

        Assert.Equal(25, birth.AgeAsOf(new DateOnly(2026, 6, 14)));
    }

    [Fact]
    public void AgeAsOf_DayAfterBirthday_IsCounted()
    {
        var birth = new BirthDate(new DateOnly(2000, 6, 15));

        Assert.Equal(26, birth.AgeAsOf(new DateOnly(2026, 6, 16)));
    }

    [Fact]
    public void AgeAsOf_BornOnLeapDay_FollowsAddYearsClamping()
    {
        var birth = new BirthDate(new DateOnly(2000, 2, 29));

        // .AddYears 在非閏年會把 2/29 夾到 2/28 (與 legacy DateTime.AddYears 相同語意)，
        // 因此非閏年時，2/28 當天即視為已過生日。
        Assert.Equal(25, birth.AgeAsOf(new DateOnly(2026, 2, 27)));
        Assert.Equal(26, birth.AgeAsOf(new DateOnly(2026, 2, 28)));
    }
}
