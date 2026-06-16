using HisModern.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HisModern.Api.Controllers;

/// <summary>病患控制器 (thin controller)。</summary>
[ApiController]
[Route("api")]
public sealed class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
        => _patientService = patientService;

    /// <summary>取得病患年齡。</summary>
    [HttpGet("patient/{id:int}/age")]
    public IActionResult Age(int id)
    {
        var result = _patientService.GetAge(id);

        if (result.IsFailure)
        {
            return Ok(new { ok = false, msg = result.Error });
        }

        var patient = result.Value!;
        return Ok(new { ok = true, name = patient.Name, age = patient.Age });
    }
}
