using HisModern.Domain.Entities;

namespace HisModern.Domain.Services;

/// <summary>
/// 掛號商業規則的單一權威來源 (domain service)。
/// 集中 legacy 散落在 God Controller 內的：晚診年齡限制、重複掛號、人數上限、序號計算。
/// 純函式、無副作用、不依賴基礎設施，方便單元測試。
/// </summary>
public sealed class RegistrationPolicy
{
    /// <summary>晚診開放掛號的最低年齡 (含)。</summary>
    public const int NightClinicMinimumAge = 12;

    /// <summary>
    /// 判斷一筆新掛號是否可被接受。
    /// </summary>
    /// <param name="patient">掛號病患。</param>
    /// <param name="session">門診時段。</param>
    /// <param name="activeRegistrations">該門診時段目前「有效」(未取消) 的掛號集合。</param>
    /// <param name="asOf">用以計算年齡的基準日期。</param>
    /// <returns>可掛號時回傳 <c>null</c>，否則回傳被拒絕的原因。</returns>
    public RegistrationDenialReason? CheckCanRegister(
        Patient patient,
        ClinicSession session,
        IReadOnlyCollection<Registration> activeRegistrations,
        DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(activeRegistrations);

        // 規則 2：晚診不接受未滿 12 歲。
        if (session.IsNightClinic && patient.AgeAsOf(asOf) < NightClinicMinimumAge)
        {
            return RegistrationDenialReason.NightClinicUnderAge;
        }

        // 規則 3：同病患在同門診時段不可重複有效掛號 (已取消者不計)。
        if (activeRegistrations.Any(r => r.PatientId == patient.Id))
        {
            return RegistrationDenialReason.DuplicateActiveRegistration;
        }

        // 規則 4：超過人數上限則額滿。
        if (activeRegistrations.Count >= session.CapacityLimit)
        {
            return RegistrationDenialReason.SessionCapacityReached;
        }

        return null;
    }

    /// <summary>
    /// 規則 5：看診序號 = 目前有效掛號數 + 1。
    /// </summary>
    public int NextSequenceNumber(IReadOnlyCollection<Registration> activeRegistrations)
    {
        ArgumentNullException.ThrowIfNull(activeRegistrations);
        return activeRegistrations.Count + 1;
    }
}
