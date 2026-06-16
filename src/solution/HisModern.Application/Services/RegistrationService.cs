using System.Globalization;
using FluentValidation;
using HisModern.Application.Abstractions;
using HisModern.Application.Common;
using HisModern.Application.Contracts;
using HisModern.Domain.Entities;
using HisModern.Domain.Enums;
using HisModern.Domain.Services;

namespace HisModern.Application.Services;

/// <summary>
/// 掛號用例的協調者。取代 legacy 的 God Controller：
/// 驗證委派給 Validator、商業規則委派給 <see cref="RegistrationPolicy"/> 與
/// <see cref="Registration"/> 實體、資料存取委派給 Repository。
/// </summary>
public sealed class RegistrationService : IRegistrationService
{
    private const string LegacyDateFormat = "yyyy/MM/dd";
    private const string LegacyDateTimeFormat = "yyyy/MM/dd HH:mm:ss";

    private readonly IPatientRepository _patients;
    private readonly IClinicSessionRepository _sessions;
    private readonly IDoctorRepository _doctors;
    private readonly IRegistrationRepository _registrations;
    private readonly RegistrationPolicy _policy;
    private readonly IValidator<CreateRegistrationRequest> _validator;
    private readonly IClock _clock;

    public RegistrationService(
        IPatientRepository patients,
        IClinicSessionRepository sessions,
        IDoctorRepository doctors,
        IRegistrationRepository registrations,
        RegistrationPolicy policy,
        IValidator<CreateRegistrationRequest> validator,
        IClock clock)
    {
        _patients = patients;
        _sessions = sessions;
        _doctors = doctors;
        _registrations = registrations;
        _policy = policy;
        _validator = validator;
        _clock = clock;
    }

    public Result<RegistrationCreatedResponse> Register(CreateRegistrationRequest? request)
    {
        // 1. 基本驗證
        if (request is null)
        {
            return Result.Failure<RegistrationCreatedResponse>(RegistrationMessages.NoBody);
        }

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            return Result.Failure<RegistrationCreatedResponse>(validation.Errors[0].ErrorMessage);
        }

        // 2. 找病患並檢核身分證
        var patient = _patients.GetById(request.PatientId);
        if (patient is null)
        {
            return Result.Failure<RegistrationCreatedResponse>(RegistrationMessages.PatientNotFound);
        }

        if (!patient.HasValidIdNumber)
        {
            return Result.Failure<RegistrationCreatedResponse>(RegistrationMessages.InvalidPatientIdNumber);
        }

        // 3. 找門診時段
        var session = _sessions.GetById(request.ScheduleId);
        if (session is null)
        {
            return Result.Failure<RegistrationCreatedResponse>(RegistrationMessages.SessionNotFound);
        }

        // 4-5. 商業規則：晚診年齡 / 重複掛號 / 人數上限 (集中於 domain policy)
        var activeRegistrations = _registrations.GetActiveBySession(session.Id);
        var denial = _policy.CheckCanRegister(patient, session, activeRegistrations, _clock.Today);
        if (denial is not null)
        {
            return Result.Failure<RegistrationCreatedResponse>(RegistrationMessages.From(denial.Value));
        }

        // 6. 產生序號並寫入
        var number = _policy.NextSequenceNumber(activeRegistrations);
        var registration = Registration.Create(
            _registrations.NextId(), patient.Id, session.Id, number, _clock.Now);
        _registrations.Add(registration);

        return Result.Success(new RegistrationCreatedResponse(registration.Id, number, session.Room));
    }

    public IReadOnlyList<RegistrationListItem> List(string? date, string? dept)
    {
        var items = new List<RegistrationListItem>();

        foreach (var registration in _registrations.GetAll())
        {
            var session = _sessions.GetById(registration.ClinicSessionId);
            if (session is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(date) && FormatDate(session.Date) != date)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(dept) && session.DepartmentCode != dept)
            {
                continue;
            }

            var patient = _patients.GetById(registration.PatientId);
            var doctor = _doctors.GetById(session.DoctorId);
            var age = patient?.AgeAsOf(_clock.Today) ?? 0;

            items.Add(new RegistrationListItem(
                GuahaoId: registration.Id,
                Patient: patient?.Name ?? "?",
                Age: age,
                Doctor: doctor?.Name ?? "?",
                Dept: session.DepartmentCode,
                Room: session.Room,
                Number: registration.Number,
                Status: (int)registration.Status,
                StatusText: registration.Status.ToDisplayText()));
        }

        return items;
    }

    public RegistrationDetailResponse? Get(int id)
    {
        var registration = _registrations.GetById(id);
        if (registration is null)
        {
            return null;
        }

        return new RegistrationDetailResponse(
            Id: registration.Id,
            PatientId: registration.PatientId,
            ScheduleId: registration.ClinicSessionId,
            Number: registration.Number,
            Status: (int)registration.Status,
            CreateTime: registration.CreatedAt.ToString(LegacyDateTimeFormat, CultureInfo.InvariantCulture));
    }

    public Result CheckIn(int id)
    {
        var registration = _registrations.GetById(id);
        if (registration is null)
        {
            return Result.Failure(RegistrationMessages.NotFound);
        }

        var error = registration.CheckIn();
        if (error is not null)
        {
            return Result.Failure(RegistrationMessages.From(error.Value));
        }

        _registrations.Update(registration);
        return Result.Success();
    }

    public Result Cancel(int id)
    {
        var registration = _registrations.GetById(id);
        if (registration is null)
        {
            return Result.Failure(RegistrationMessages.NotFound);
        }

        registration.Cancel();
        _registrations.Update(registration);
        return Result.Success();
    }

    private static string FormatDate(DateOnly date)
        => date.ToString(LegacyDateFormat, CultureInfo.InvariantCulture);
}
