using HisModern.Domain.Enums;

namespace HisModern.Domain.Entities;

/// <summary>
/// 門診時段 (門診時段 / ClinicSession)。取代 legacy 的 <c>Menzhen</c>。
/// 一個醫師、某日期、某診次的看診時段，並帶有人數上限。
/// </summary>
public sealed class ClinicSession
{
    public ClinicSession(int id, int doctorId, string departmentCode, DateOnly date,
        ClinicPeriod period, string room, int capacityLimit)
    {
        Id = id;
        DoctorId = doctorId;
        DepartmentCode = departmentCode;
        Date = date;
        Period = period;
        Room = room;
        CapacityLimit = capacityLimit;
    }

    public int Id { get; }
    public int DoctorId { get; }
    public string DepartmentCode { get; }
    public DateOnly Date { get; }
    public ClinicPeriod Period { get; }
    public string Room { get; }

    /// <summary>該診次可掛號人數上限。</summary>
    public int CapacityLimit { get; }

    /// <summary>是否為晚診 (受年齡限制的診次)。</summary>
    public bool IsNightClinic => Period == ClinicPeriod.Night;
}
