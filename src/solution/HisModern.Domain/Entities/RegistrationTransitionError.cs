namespace HisModern.Domain.Entities;

/// <summary>
/// 掛號狀態轉換失敗的原因。由 <see cref="Registration"/> 的轉換方法回傳，
/// 取代 legacy 在 Controller 內以字串硬寫錯誤訊息的做法。
/// </summary>
public enum RegistrationTransitionError
{
    /// <summary>已取消的掛號無法報到。</summary>
    CancelledCannotCheckIn,

    /// <summary>已看診完成的掛號無法報到。</summary>
    CompletedCannotCheckIn,

    /// <summary>已報到，無法重複報到。</summary>
    AlreadyCheckedIn
}
