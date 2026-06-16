using HisModern.Application.Contracts;
using HisModern.Tests.Common;

namespace HisModern.Tests.Application;

/// <summary>今日門診統計報表。</summary>
public sealed class ReportServiceTests
{
    private const int Session_內科早診 = 1;

    [Fact]
    public void TodayReport_CountsTotalCheckedInAndCancelled()
    {
        var harness = TestHarness.Create();

        var a = harness.RegistrationService.Register(new CreateRegistrationRequest(1, Session_內科早診));
        var b = harness.RegistrationService.Register(new CreateRegistrationRequest(2, Session_內科早診));
        harness.RegistrationService.Register(new CreateRegistrationRequest(3, Session_內科早診));

        harness.RegistrationService.CheckIn(a.Value!.GuahaoId); // 1 筆報到
        harness.RegistrationService.Cancel(b.Value!.GuahaoId);  // 1 筆取消

        var report = harness.ReportService.GetTodayReport();

        Assert.Equal(harness.Clock.Today.ToString("yyyy/MM/dd"), report.Date);
        Assert.Equal(3, report.Total);   // 含已取消，total 計入所有當日掛號
        Assert.Equal(1, report.Baodao);
        Assert.Equal(1, report.Cancel);
    }

    [Fact]
    public void TodayReport_IsZero_WhenNoRegistrations()
    {
        var harness = TestHarness.Create();

        var report = harness.ReportService.GetTodayReport();

        Assert.Equal(0, report.Total);
        Assert.Equal(0, report.Baodao);
        Assert.Equal(0, report.Cancel);
    }
}
