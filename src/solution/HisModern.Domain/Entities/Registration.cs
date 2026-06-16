using HisModern.Domain.Enums;

namespace HisModern.Domain.Entities;

/// <summary>
/// 掛號紀錄 (掛號 / Registration)。取代 legacy 的 <c>Guahao</c>。
/// 狀態轉換規則內聚在實體本身，外部無法任意改寫 <see cref="Status"/>。
/// </summary>
public sealed class Registration
{
    private Registration(int id, int patientId, int clinicSessionId, int number,
        RegistrationStatus status, DateTime createdAt)
    {
        Id = id;
        PatientId = patientId;
        ClinicSessionId = clinicSessionId;
        Number = number;
        Status = status;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public int PatientId { get; }
    public int ClinicSessionId { get; }

    /// <summary>看診序號。</summary>
    public int Number { get; }

    public RegistrationStatus Status { get; private set; }
    public DateTime CreatedAt { get; }

    /// <summary>
    /// 是否為「有效」掛號 (未取消)。重複掛號檢查與人數上限計算皆以此為準
    /// (對齊 legacy 的 status != 3 判斷，已取消不計入)。
    /// </summary>
    public bool IsActive => Status != RegistrationStatus.Cancelled;

    /// <summary>建立一筆新掛號 (初始狀態為已掛號)。</summary>
    public static Registration Create(int id, int patientId, int clinicSessionId, int number, DateTime createdAt)
        => new(id, patientId, clinicSessionId, number, RegistrationStatus.Registered, createdAt);

    /// <summary>由既有資料重建掛號實體 (供 Repository 還原狀態使用)。</summary>
    public static Registration Restore(int id, int patientId, int clinicSessionId, int number,
        RegistrationStatus status, DateTime createdAt)
        => new(id, patientId, clinicSessionId, number, status, createdAt);

    /// <summary>
    /// 報到。商業規則：只允許「已掛號」轉為「已報到」。
    /// 失敗時回傳原因，成功時回傳 null。
    /// </summary>
    public RegistrationTransitionError? CheckIn()
    {
        switch (Status)
        {
            case RegistrationStatus.Cancelled:
                return RegistrationTransitionError.CancelledCannotCheckIn;
            case RegistrationStatus.Completed:
                return RegistrationTransitionError.CompletedCannotCheckIn;
            case RegistrationStatus.CheckedIn:
                return RegistrationTransitionError.AlreadyCheckedIn;
            default:
                Status = RegistrationStatus.CheckedIn;
                return null;
        }
    }

    /// <summary>取消掛號。任何狀態皆可取消，最終狀態為已取消 (對齊 legacy 行為)。</summary>
    public void Cancel() => Status = RegistrationStatus.Cancelled;

    /// <summary>標記為看診完成 (只允許自已報到轉換)。保留供未來流程使用。</summary>
    public bool Complete()
    {
        if (Status != RegistrationStatus.CheckedIn)
        {
            return false;
        }

        Status = RegistrationStatus.Completed;
        return true;
    }
}
