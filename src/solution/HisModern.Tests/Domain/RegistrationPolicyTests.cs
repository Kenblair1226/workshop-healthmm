using HisModern.Domain.Entities;
using HisModern.Domain.Enums;
using HisModern.Domain.Services;
using HisModern.Domain.ValueObjects;

namespace HisModern.Tests.Domain;

/// <summary>
/// 商業規則 2/3/4/5 的核心：晚診年齡限制、重複掛號、人數上限、序號計算。
/// </summary>
public sealed class RegistrationPolicyTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);
    private readonly RegistrationPolicy _policy = new();

    private static Patient PatientBornOn(int id, DateOnly birth)
        => new(id, $"P{id}", Gender.Male, new BirthDate(birth), "A123456789", null);

    private static ClinicSession Session(ClinicPeriod period, int limit = 30)
        => new(1, doctorId: 1, departmentCode: "PED", date: Today, period: period, room: "401", capacityLimit: limit);

    private static Registration ActiveFor(int patientId)
        => Registration.Create(2000 + patientId, patientId, clinicSessionId: 1, number: patientId, createdAt: DateTime.Now);

    // ---- 規則 2：晚診 12 歲以下 ----

    [Fact]
    public void NightClinic_ExactlyTwelve_IsAllowed()
    {
        // 今天剛好滿 12 歲 (生日當天)。
        var patient = PatientBornOn(1, new DateOnly(2014, 6, 15));

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Night), Array.Empty<Registration>(), Today);

        Assert.Null(denial);
    }

    [Fact]
    public void NightClinic_OneDayShortOfTwelve_IsRejected()
    {
        // 生日晚一天 → 今天仍為 11 歲。
        var patient = PatientBornOn(1, new DateOnly(2014, 6, 16));

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Night), Array.Empty<Registration>(), Today);

        Assert.Equal(RegistrationDenialReason.NightClinicUnderAge, denial);
    }

    [Fact]
    public void NightClinic_Eleven_IsRejected()
    {
        var patient = PatientBornOn(1, new DateOnly(2015, 6, 15)); // 11 歲

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Night), Array.Empty<Registration>(), Today);

        Assert.Equal(RegistrationDenialReason.NightClinicUnderAge, denial);
    }

    [Fact]
    public void NonNightClinic_UnderTwelve_IsAllowed()
    {
        var patient = PatientBornOn(1, new DateOnly(2020, 1, 20)); // 6 歲

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Morning), Array.Empty<Registration>(), Today);

        Assert.Null(denial);
    }

    // ---- 規則 3：重複掛號 ----

    [Fact]
    public void DuplicateActiveRegistration_IsRejected()
    {
        var patient = PatientBornOn(1, new DateOnly(1985, 3, 12));
        var active = new[] { ActiveFor(patientId: 1) };

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Morning), active, Today);

        Assert.Equal(RegistrationDenialReason.DuplicateActiveRegistration, denial);
    }

    [Fact]
    public void DifferentPatient_SameSession_IsNotDuplicate()
    {
        var patient = PatientBornOn(2, new DateOnly(1985, 3, 12));
        var active = new[] { ActiveFor(patientId: 1) };

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Morning), active, Today);

        Assert.Null(denial);
    }

    // ---- 規則 4：人數上限 (邊界) ----

    [Fact]
    public void Capacity_WhenActiveCountReachesLimit_IsRejected()
    {
        var patient = PatientBornOn(99, new DateOnly(1985, 3, 12));
        var active = new[] { ActiveFor(1), ActiveFor(2) }; // 已有 2 筆

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Morning, limit: 2), active, Today);

        Assert.Equal(RegistrationDenialReason.SessionCapacityReached, denial);
    }

    [Fact]
    public void Capacity_OneBelowLimit_IsAllowed()
    {
        var patient = PatientBornOn(99, new DateOnly(1985, 3, 12));
        var active = new[] { ActiveFor(1) }; // 已有 1 筆，上限 2

        var denial = _policy.CheckCanRegister(patient, Session(ClinicPeriod.Morning, limit: 2), active, Today);

        Assert.Null(denial);
    }

    // ---- 規則 5：序號 = 有效掛號數 + 1 ----

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(5, 6)]
    public void NextSequenceNumber_IsActiveCountPlusOne(int activeCount, int expected)
    {
        var active = Enumerable.Range(1, activeCount).Select(ActiveFor).ToArray();

        Assert.Equal(expected, _policy.NextSequenceNumber(active));
    }
}
