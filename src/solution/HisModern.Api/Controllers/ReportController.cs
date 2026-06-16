using HisModern.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HisModern.Api.Controllers;

/// <summary>報表控制器 (thin controller)。</summary>
[ApiController]
[Route("api")]
public sealed class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
        => _reportService = reportService;

    /// <summary>今日門診統計報表。</summary>
    [HttpGet("report/today")]
    public IActionResult Today() => Ok(_reportService.GetTodayReport());
}
