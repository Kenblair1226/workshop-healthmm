using HisModern.Application.Common;
using HisModern.Application.Contracts;

namespace HisModern.Application.Services;

/// <summary>掛號相關用例。</summary>
public interface IRegistrationService
{
    /// <summary>新增掛號。</summary>
    Result<RegistrationCreatedResponse> Register(CreateRegistrationRequest? request);

    /// <summary>查詢掛號清單，可選日期 ("yyyy/MM/dd") 與科別代碼篩選。</summary>
    IReadOnlyList<RegistrationListItem> List(string? date, string? dept);

    /// <summary>查詢單筆掛號明細；查無資料回傳 null。</summary>
    RegistrationDetailResponse? Get(int id);

    /// <summary>報到。</summary>
    Result CheckIn(int id);

    /// <summary>取消掛號。</summary>
    Result Cancel(int id);
}

/// <summary>病患相關用例。</summary>
public interface IPatientService
{
    /// <summary>取得病患年齡。</summary>
    Result<PatientAgeResponse> GetAge(int id);
}

/// <summary>報表相關用例。</summary>
public interface IReportService
{
    /// <summary>今日門診統計。</summary>
    TodayReportResponse GetTodayReport();
}
