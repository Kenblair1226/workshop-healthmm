using FluentValidation;
using HisModern.Application.Contracts;
using HisModern.Application.Services;
using HisModern.Application.Validation;
using HisModern.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HisModern.Application;

/// <summary>Application 層的 DI 註冊。每層各自提供 AddXxx 擴充方法。</summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Domain service (純函式、無狀態) 以 singleton 註冊。
        services.AddSingleton<RegistrationPolicy>();

        // 驗證器。
        services.AddScoped<IValidator<CreateRegistrationRequest>, CreateRegistrationRequestValidator>();

        // 用例服務。
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
