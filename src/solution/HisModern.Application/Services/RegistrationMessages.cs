using HisModern.Domain.Entities;
using HisModern.Domain.Services;

namespace HisModern.Application.Services;

/// <summary>
/// 對外使用者訊息的單一來源。集中 legacy 散落在 Controller 各處的硬寫字串，
/// 並保持與 legacy 完全一致以維持前端契約。
/// </summary>
public static class RegistrationMessages
{
    public const string NoBody = "no body";
    public const string PatientIdRequired = "patientId 不可空白";
    public const string ScheduleIdRequired = "scheduleId 不可空白";
    public const string PatientNotFound = "查無此病患";
    public const string InvalidPatientIdNumber = "病患身分證資料有誤";
    public const string SessionNotFound = "查無此門診時段";
    public const string NightClinicUnderAge = "晚診不開放 12 歲以下掛號";
    public const string DuplicateRegistration = "重複掛號";
    public const string SessionFull = "額滿";
    public const string NotFound = "查無資料";
    public const string CancelledCannotCheckIn = "已取消無法報到";
    public const string CompletedCannotCheckIn = "已看診完成";
    public const string AlreadyCheckedIn = "已報到，無法重複報到";
    public const string Unknown = "未知錯誤";

    /// <summary>將 domain 拒絕原因對應為使用者訊息。</summary>
    public static string From(RegistrationDenialReason reason) => reason switch
    {
        RegistrationDenialReason.NightClinicUnderAge => NightClinicUnderAge,
        RegistrationDenialReason.DuplicateActiveRegistration => DuplicateRegistration,
        RegistrationDenialReason.SessionCapacityReached => SessionFull,
        _ => Unknown
    };

    /// <summary>將 domain 狀態轉換錯誤對應為使用者訊息。</summary>
    public static string From(RegistrationTransitionError error) => error switch
    {
        RegistrationTransitionError.CancelledCannotCheckIn => CancelledCannotCheckIn,
        RegistrationTransitionError.CompletedCannotCheckIn => CompletedCannotCheckIn,
        RegistrationTransitionError.AlreadyCheckedIn => AlreadyCheckedIn,
        _ => Unknown
    };
}
