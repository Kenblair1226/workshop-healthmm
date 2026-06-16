using HisModern.Application.Abstractions;
using HisModern.Application.Common;
using HisModern.Application.Contracts;

namespace HisModern.Application.Services;

/// <summary>病患用例。年齡計算統一委派給 domain 的 <c>Patient.AgeAsOf</c>。</summary>
public sealed class PatientService : IPatientService
{
    private readonly IPatientRepository _patients;
    private readonly IClock _clock;

    public PatientService(IPatientRepository patients, IClock clock)
    {
        _patients = patients;
        _clock = clock;
    }

    public Result<PatientAgeResponse> GetAge(int id)
    {
        var patient = _patients.GetById(id);
        if (patient is null)
        {
            return Result.Failure<PatientAgeResponse>(RegistrationMessages.PatientNotFound);
        }

        return Result.Success(new PatientAgeResponse(patient.Name, patient.AgeAsOf(_clock.Today)));
    }
}
