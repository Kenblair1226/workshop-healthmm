using HisModern.Application.Contracts;
using HisModern.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HisModern.Api.Controllers;

/// <summary>
/// 門診掛號控制器 (thin controller)。
/// 僅負責 HTTP 轉接：解析請求、呼叫 Application 服務、組裝回應。
/// 所有商業邏輯已下放至 Application / Domain 層。
/// 為維持與 legacy 完全相同的對外 JSON 契約 (前端不需修改)，
/// 業務失敗仍以 200 + { ok:false, msg } 形式回傳。
/// </summary>
[ApiController]
[Route("api")]
public sealed class GuahaoController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public GuahaoController(IRegistrationService registrationService)
        => _registrationService = registrationService;

    /// <summary>新增掛號。</summary>
    [HttpPost("guahao")]
    public IActionResult Create([FromBody] CreateRegistrationRequest? request)
    {
        var result = _registrationService.Register(request);

        if (result.IsFailure)
        {
            return Ok(new { ok = false, msg = result.Error });
        }

        var created = result.Value!;
        return Ok(new { ok = true, guahaoId = created.GuahaoId, number = created.Number, room = created.Room });
    }

    /// <summary>查詢掛號清單 (可選 date / dept 篩選)。</summary>
    [HttpGet("guahao")]
    public IActionResult List([FromQuery] string? date, [FromQuery] string? dept)
        => Ok(_registrationService.List(date, dept));

    /// <summary>查詢單筆掛號。</summary>
    [HttpGet("guahao/{id:int}")]
    public IActionResult Get(int id)
    {
        var detail = _registrationService.Get(id);
        return detail is null
            ? Ok(new { ok = false, msg = RegistrationMessages.NotFound })
            : Ok(detail);
    }

    /// <summary>報到。</summary>
    [HttpPost("guahao/{id:int}/baodao")]
    public IActionResult CheckIn(int id)
    {
        var result = _registrationService.CheckIn(id);
        return result.IsSuccess
            ? Ok(new { ok = true })
            : Ok(new { ok = false, msg = result.Error });
    }

    /// <summary>取消掛號。</summary>
    [HttpPost("guahao/{id:int}/cancel")]
    public IActionResult Cancel(int id)
    {
        var result = _registrationService.Cancel(id);
        return result.IsSuccess
            ? Ok(new { ok = true })
            : Ok(new { ok = false, msg = result.Error });
    }
}
