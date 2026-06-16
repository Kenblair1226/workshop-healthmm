using HisModern.Application.Abstractions;
using HisModern.Application.Services;
using HisModern.Application.Validation;
using HisModern.Domain.Services;
using HisModern.Infrastructure.Persistence;
using HisModern.Infrastructure.Repositories;

namespace HisModern.Tests.Common;

/// <summary>
/// 以真實的 in-memory Repository、Domain policy 與 Validator 組裝 Application 服務，
/// 進行貼近真實流程的整合型單元測試。時鐘可注入以固定「今天」。
/// </summary>
public sealed class TestHarness
{
    /// <summary>測試預設基準日 (種子門診時段會以此日期建立)。</summary>
    public static readonly DateOnly DefaultToday = new(2026, 6, 15);

    private TestHarness(
        InMemoryDataStore store,
        IClock clock,
        IRegistrationRepository registrations,
        RegistrationService registrationService,
        PatientService patientService,
        ReportService reportService)
    {
        Store = store;
        Clock = clock;
        Registrations = registrations;
        RegistrationService = registrationService;
        PatientService = patientService;
        ReportService = reportService;
    }

    public InMemoryDataStore Store { get; }
    public IClock Clock { get; }
    public IRegistrationRepository Registrations { get; }
    public RegistrationService RegistrationService { get; }
    public PatientService PatientService { get; }
    public ReportService ReportService { get; }

    public static TestHarness Create(DateOnly? today = null)
    {
        var clock = new FixedClock(today ?? DefaultToday);
        var store = new InMemoryDataStore(clock);

        var patients = new PatientRepository(store);
        var sessions = new ClinicSessionRepository(store);
        var doctors = new DoctorRepository(store);
        var registrations = new RegistrationRepository(store);

        var policy = new RegistrationPolicy();
        var validator = new CreateRegistrationRequestValidator();

        var registrationService = new RegistrationService(
            patients, sessions, doctors, registrations, policy, validator, clock);
        var patientService = new PatientService(patients, clock);
        var reportService = new ReportService(registrations, sessions, clock);

        return new TestHarness(store, clock, registrations,
            registrationService, patientService, reportService);
    }
}
