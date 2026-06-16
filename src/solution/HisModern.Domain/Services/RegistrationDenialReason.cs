namespace HisModern.Domain.Services;

/// <summary>
/// 掛號被拒絕的原因。由 <see cref="RegistrationPolicy"/> 回傳，
/// 由上層 (Application) 對應為使用者可見訊息，維持 domain 與訊息文案解耦。
/// </summary>
public enum RegistrationDenialReason
{
    /// <summary>晚診不開放未滿 12 歲掛號。</summary>
    NightClinicUnderAge,

    /// <summary>同一病患在同一門診時段已有有效掛號 (重複掛號)。</summary>
    DuplicateActiveRegistration,

    /// <summary>該門診時段已達人數上限 (額滿)。</summary>
    SessionCapacityReached
}
