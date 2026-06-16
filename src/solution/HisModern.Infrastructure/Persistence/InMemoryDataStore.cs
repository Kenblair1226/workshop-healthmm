using System.Collections.Concurrent;
using HisModern.Application.Abstractions;
using HisModern.Domain.Entities;
using HisModern.Domain.Enums;
using HisModern.Domain.ValueObjects;

namespace HisModern.Infrastructure.Persistence;

/// <summary>
/// 記憶體資料儲存。取代 legacy 的 static <c>DB</c> 類別：
/// 以 <see cref="ConcurrentDictionary{TKey,TValue}"/> 與 <see cref="Interlocked"/> 提供執行緒安全，
/// 並以單例 (singleton) 注入取代全域靜態狀態。
/// 種子資料 (seed) 與 legacy <c>DB.Seed()</c> 完全一致。
/// </summary>
public sealed class InMemoryDataStore
{
    // legacy: seqGuahao = 1000，第一筆掛號 id 取用 1000。
    // 以 Interlocked.Increment 取得執行緒安全流水號，故初始值設為 999。
    private const int RegistrationSeedStart = 999;

    private int _registrationSequence = RegistrationSeedStart;

    public ConcurrentDictionary<int, Patient> Patients { get; } = new();
    public ConcurrentDictionary<string, Department> Departments { get; } = new();
    public ConcurrentDictionary<int, Doctor> Doctors { get; } = new();
    public ConcurrentDictionary<int, ClinicSession> ClinicSessions { get; } = new();
    public ConcurrentDictionary<int, Registration> Registrations { get; } = new();

    public InMemoryDataStore(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        Seed(clock);
    }

    /// <summary>取得下一個掛號流水號 (執行緒安全)，取代 legacy 無 lock 的 seqGuahao++。</summary>
    public int NextRegistrationId() => Interlocked.Increment(ref _registrationSequence);

    private void Seed(IClock clock)
    {
        // 科別 (對齊 legacy DB.Seed)
        AddDepartment(new Department("INT", "內科"));
        AddDepartment(new Department("SUR", "外科"));
        AddDepartment(new Department("PED", "小兒科"));
        AddDepartment(new Department("ENT", "耳鼻喉科"));

        // 醫師
        AddDoctor(new Doctor(1, "王大明", "INT"));
        AddDoctor(new Doctor(2, "李美麗", "INT"));
        AddDoctor(new Doctor(3, "陳志強", "SUR"));
        AddDoctor(new Doctor(4, "林淑芬", "PED"));

        // 門診時段 (日期寫死為「今天」，與 legacy 相同；此處改由可注入的時鐘提供)
        var today = clock.Today;
        AddClinicSession(new ClinicSession(1, 1, "INT", today, ClinicPeriod.Morning, "201", 30));
        AddClinicSession(new ClinicSession(2, 2, "INT", today, ClinicPeriod.Afternoon, "202", 20));
        AddClinicSession(new ClinicSession(3, 3, "SUR", today, ClinicPeriod.Morning, "301", 15));
        AddClinicSession(new ClinicSession(4, 4, "PED", today, ClinicPeriod.Night, "401", 25));

        // 病患 (保留 legacy 原始性別/生日格式，於建構時正規化)
        AddPatient(new Patient(1, "張三", GenderExtensions.FromLegacyString("M"),
            BirthDate.Parse("1985/03/12"), "A123456789", "0912345678"));
        AddPatient(new Patient(2, "李四", GenderExtensions.FromLegacyString("女"),
            BirthDate.Parse("1990-07-08"), "B223456788", "0922333444"));
        AddPatient(new Patient(3, "小明", GenderExtensions.FromLegacyString("M"),
            BirthDate.Parse("2020/01/20"), "C123456780", "0933000111"));
    }

    private void AddDepartment(Department department) => Departments[department.Code] = department;
    private void AddDoctor(Doctor doctor) => Doctors[doctor.Id] = doctor;
    private void AddClinicSession(ClinicSession session) => ClinicSessions[session.Id] = session;
    private void AddPatient(Patient patient) => Patients[patient.Id] = patient;
}
