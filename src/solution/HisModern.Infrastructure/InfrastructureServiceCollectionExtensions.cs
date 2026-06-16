using HisModern.Application.Abstractions;
using HisModern.Infrastructure.Persistence;
using HisModern.Infrastructure.Repositories;
using HisModern.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace HisModern.Infrastructure;

/// <summary>Infrastructure 層的 DI 註冊。</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // 時鐘與記憶體資料儲存皆為共享狀態，註冊為 singleton。
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<InMemoryDataStore>();

        // Repository 為 InMemoryDataStore 的薄包裝，可註冊為 singleton。
        services.AddSingleton<IPatientRepository, PatientRepository>();
        services.AddSingleton<IDepartmentRepository, DepartmentRepository>();
        services.AddSingleton<IDoctorRepository, DoctorRepository>();
        services.AddSingleton<IClinicSessionRepository, ClinicSessionRepository>();
        services.AddSingleton<IRegistrationRepository, RegistrationRepository>();

        return services;
    }
}
