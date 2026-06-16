using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace HisModern.Api.Infrastructure;

/// <summary>
/// 全域例外處理器。取代 legacy「吞例外後回傳 200 + ERROR 字串」的反模式，
/// 改以 RFC 7807 ProblemDetails (HTTP 500) 回報非預期錯誤。
/// 注意：可預期的業務失敗並不會走到這裡，而是由各服務以 Result 明確回傳。
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "未處理的例外: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "伺服器發生未預期的錯誤",
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1"
            }
        });
    }
}
