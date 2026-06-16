using System.Globalization;
using HisModern.Application.Abstractions;
using HisModern.Application.Contracts;
using HisModern.Domain.Enums;

namespace HisModern.Application.Services;

/// <summary>今日門診統計用例。</summary>
public sealed class ReportService : IReportService
{
    private const string LegacyDateFormat = "yyyy/MM/dd";

    private readonly IRegistrationRepository _registrations;
    private readonly IClinicSessionRepository _sessions;
    private readonly IClock _clock;

    public ReportService(
        IRegistrationRepository registrations,
        IClinicSessionRepository sessions,
        IClock clock)
    {
        _registrations = registrations;
        _sessions = sessions;
        _clock = clock;
    }

    public TodayReportResponse GetTodayReport()
    {
        var today = _clock.Today;
        var total = 0;
        var checkedIn = 0;
        var cancelled = 0;

        foreach (var registration in _registrations.GetAll())
        {
            var session = _sessions.GetById(registration.ClinicSessionId);
            if (session is null || session.Date != today)
            {
                continue;
            }

            total++;
            if (registration.Status == RegistrationStatus.CheckedIn)
            {
                checkedIn++;
            }

            if (registration.Status == RegistrationStatus.Cancelled)
            {
                cancelled++;
            }
        }

        return new TodayReportResponse(
            Date: today.ToString(LegacyDateFormat, CultureInfo.InvariantCulture),
            Total: total,
            Baodao: checkedIn,
            Cancel: cancelled);
    }
}
