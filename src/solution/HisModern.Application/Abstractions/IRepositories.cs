using HisModern.Domain.Entities;

namespace HisModern.Application.Abstractions;

/// <summary>病患資料存取。實作位於 Infrastructure 層。</summary>
public interface IPatientRepository
{
    Patient? GetById(int id);
    IReadOnlyCollection<Patient> GetAll();
}

/// <summary>科別資料存取。</summary>
public interface IDepartmentRepository
{
    Department? GetByCode(string code);
    IReadOnlyCollection<Department> GetAll();
}

/// <summary>醫師資料存取。</summary>
public interface IDoctorRepository
{
    Doctor? GetById(int id);
    IReadOnlyCollection<Doctor> GetAll();
}

/// <summary>門診時段資料存取。</summary>
public interface IClinicSessionRepository
{
    ClinicSession? GetById(int id);
    IReadOnlyCollection<ClinicSession> GetAll();
}

/// <summary>掛號資料存取。</summary>
public interface IRegistrationRepository
{
    Registration? GetById(int id);

    IReadOnlyCollection<Registration> GetAll();

    /// <summary>取得某門診時段目前「有效」(未取消) 的掛號。</summary>
    IReadOnlyCollection<Registration> GetActiveBySession(int clinicSessionId);

    /// <summary>取得下一個掛號流水號 (執行緒安全)。</summary>
    int NextId();

    void Add(Registration registration);

    void Update(Registration registration);
}
