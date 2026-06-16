using HisModern.Application.Abstractions;
using HisModern.Domain.Entities;
using HisModern.Infrastructure.Persistence;

namespace HisModern.Infrastructure.Repositories;

public sealed class PatientRepository : IPatientRepository
{
    private readonly InMemoryDataStore _store;

    public PatientRepository(InMemoryDataStore store) => _store = store;

    public Patient? GetById(int id) => _store.Patients.GetValueOrDefault(id);

    public IReadOnlyCollection<Patient> GetAll() => _store.Patients.Values.ToList();
}

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly InMemoryDataStore _store;

    public DepartmentRepository(InMemoryDataStore store) => _store = store;

    public Department? GetByCode(string code) => _store.Departments.GetValueOrDefault(code);

    public IReadOnlyCollection<Department> GetAll() => _store.Departments.Values.ToList();
}

public sealed class DoctorRepository : IDoctorRepository
{
    private readonly InMemoryDataStore _store;

    public DoctorRepository(InMemoryDataStore store) => _store = store;

    public Doctor? GetById(int id) => _store.Doctors.GetValueOrDefault(id);

    public IReadOnlyCollection<Doctor> GetAll() => _store.Doctors.Values.ToList();
}

public sealed class ClinicSessionRepository : IClinicSessionRepository
{
    private readonly InMemoryDataStore _store;

    public ClinicSessionRepository(InMemoryDataStore store) => _store = store;

    public ClinicSession? GetById(int id) => _store.ClinicSessions.GetValueOrDefault(id);

    public IReadOnlyCollection<ClinicSession> GetAll() => _store.ClinicSessions.Values.ToList();
}

public sealed class RegistrationRepository : IRegistrationRepository
{
    private readonly InMemoryDataStore _store;

    public RegistrationRepository(InMemoryDataStore store) => _store = store;

    public Registration? GetById(int id) => _store.Registrations.GetValueOrDefault(id);

    public IReadOnlyCollection<Registration> GetAll() => _store.Registrations.Values.ToList();

    public IReadOnlyCollection<Registration> GetActiveBySession(int clinicSessionId) =>
        _store.Registrations.Values
            .Where(r => r.ClinicSessionId == clinicSessionId && r.IsActive)
            .ToList();

    public int NextId() => _store.NextRegistrationId();

    public void Add(Registration registration) => _store.Registrations[registration.Id] = registration;

    public void Update(Registration registration) => _store.Registrations[registration.Id] = registration;
}
