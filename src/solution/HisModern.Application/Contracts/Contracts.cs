namespace HisModern.Application.Contracts;

/// <summary>
/// 新增掛號請求。對應 legacy front-end 送出的 JSON：{ patientId, scheduleId }。
/// </summary>
public sealed record CreateRegistrationRequest(int PatientId, int ScheduleId);

/// <summary>
/// 掛號成功回應。序列化後對應 legacy 的 { guahaoId, number, room }
/// (外層 { ok = true } 由 Controller 包裝以維持契約)。
/// </summary>
public sealed record RegistrationCreatedResponse(int GuahaoId, int Number, string Room);

/// <summary>
/// 掛號清單項目。序列化欄位名需與 legacy 完全一致：
/// guahaoId, patient, age, doctor, dept, room, number, status, statusText。
/// </summary>
public sealed record RegistrationListItem(
    int GuahaoId,
    string Patient,
    int Age,
    string Doctor,
    string Dept,
    string Room,
    int Number,
    int Status,
    string StatusText);

/// <summary>
/// 單筆掛號明細。序列化欄位名需與 legacy 的 Guahao 一致：
/// id, patientId, scheduleId, number, status, createTime。
/// </summary>
public sealed record RegistrationDetailResponse(
    int Id,
    int PatientId,
    int ScheduleId,
    int Number,
    int Status,
    string CreateTime);

/// <summary>病患年齡查詢結果 (外層 { ok = true } 由 Controller 包裝)。</summary>
public sealed record PatientAgeResponse(string Name, int Age);

/// <summary>今日門診統計。序列化欄位名需與 legacy 一致：date, total, baodao, cancel。</summary>
public sealed record TodayReportResponse(string Date, int Total, int Baodao, int Cancel);
