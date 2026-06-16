using HisModern.Application.Contracts;
using HisModern.Application.Services;
using HisModern.Domain.Entities;
using HisModern.Domain.Enums;
using HisModern.Domain.ValueObjects;
using HisModern.Tests.Common;

namespace HisModern.Tests.Application;

/// <summary>
/// 以真實 in-memory 流程驗證所有六項商業規則與輸入驗證 / 邊界。
/// </summary>
public sealed class RegistrationServiceTests
{
    private const int Patient_張三 = 1; // 1985/03/12，成人
    private const int Patient_李四 = 2; // 1990-07-08，成人
    private const int Patient_小明 = 3; // 2020/01/20，6 歲

    private const int Session_內科早診 = 1; // room 201, limit 30
    private const int Session_小兒科晚診 = 4; // night, room 401, limit 25

    // ---- 成功路徑 + 規則 5 (序號) ----

    [Fact]
    public void Register_Succeeds_WithFirstSequenceNumberAndRoom()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Number);
        Assert.Equal("201", result.Value.Room);
    }

    [Fact]
    public void Register_SecondPatient_GetsNextSequenceNumber()
    {
        var harness = TestHarness.Create();

        harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));
        var second = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_李四, Session_內科早診));

        Assert.True(second.IsSuccess);
        Assert.Equal(2, second.Value!.Number);
    }

    // ---- 規則 3：重複掛號 ----

    [Fact]
    public void Register_DuplicateActiveRegistration_IsRejected()
    {
        var harness = TestHarness.Create();

        harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));
        var duplicate = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        Assert.True(duplicate.IsFailure);
        Assert.Equal(RegistrationMessages.DuplicateRegistration, duplicate.Error);
    }

    [Fact]
    public void Register_AfterCancel_IsAllowedAgain()
    {
        var harness = TestHarness.Create();

        var first = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));
        harness.RegistrationService.Cancel(first.Value!.GuahaoId);

        var again = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        Assert.True(again.IsSuccess);
        // 取消後再掛，有效掛號數歸零，序號重新從 1 開始。
        Assert.Equal(1, again.Value!.Number);
    }

    // ---- 規則 2：晚診 12 歲以下 ----

    [Fact]
    public void Register_NightClinic_RejectsChildUnderTwelve()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_小明, Session_小兒科晚診));

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.NightClinicUnderAge, result.Error);
    }

    [Fact]
    public void Register_NightClinic_AllowsAdult()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_小兒科晚診));

        Assert.True(result.IsSuccess);
    }

    // ---- 規則 4：人數上限 (邊界) ----

    [Fact]
    public void Register_WhenSessionFull_IsRejected()
    {
        var harness = TestHarness.Create();
        const int fullSessionId = 9;
        harness.Store.ClinicSessions[fullSessionId] = new ClinicSession(
            fullSessionId, doctorId: 1, departmentCode: "INT", date: harness.Clock.Today,
            period: ClinicPeriod.Morning, room: "999", capacityLimit: 1);

        var first = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, fullSessionId));
        var overflow = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_李四, fullSessionId));

        Assert.True(first.IsSuccess);
        Assert.True(overflow.IsFailure);
        Assert.Equal(RegistrationMessages.SessionFull, overflow.Error);
    }

    // ---- 輸入驗證 ----

    [Fact]
    public void Register_NullRequest_ReturnsNoBody()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(null);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.NoBody, result.Error);
    }

    [Fact]
    public void Register_InvalidPatientId_ReturnsValidationMessage()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(0, Session_內科早診));

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.PatientIdRequired, result.Error);
    }

    [Fact]
    public void Register_InvalidScheduleId_ReturnsValidationMessage()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, 0));

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.ScheduleIdRequired, result.Error);
    }

    [Fact]
    public void Register_UnknownPatient_ReturnsNotFound()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(404, Session_內科早診));

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.PatientNotFound, result.Error);
    }

    [Fact]
    public void Register_PatientWithInvalidIdNumber_IsRejected()
    {
        var harness = TestHarness.Create();
        harness.Store.Patients[99] = new Patient(
            99, "壞資料", Gender.Unknown, new BirthDate(new DateOnly(1980, 1, 1)), "BAD", null);

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(99, Session_內科早診));

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.InvalidPatientIdNumber, result.Error);
    }

    [Fact]
    public void Register_UnknownSession_ReturnsNotFound()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, 404));

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.SessionNotFound, result.Error);
    }

    // ---- 規則 6：報到 / 取消狀態轉換 ----

    [Fact]
    public void CheckIn_FromRegistered_Succeeds_AndUpdatesStatus()
    {
        var harness = TestHarness.Create();
        var created = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        var result = harness.RegistrationService.CheckIn(created.Value!.GuahaoId);

        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatus.CheckedIn, harness.Registrations.GetById(created.Value.GuahaoId)!.Status);
    }

    [Fact]
    public void CheckIn_UnknownRegistration_ReturnsNotFound()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.CheckIn(12345);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.NotFound, result.Error);
    }

    [Fact]
    public void CheckIn_AfterCancel_IsRejected()
    {
        var harness = TestHarness.Create();
        var created = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));
        harness.RegistrationService.Cancel(created.Value!.GuahaoId);

        var result = harness.RegistrationService.CheckIn(created.Value.GuahaoId);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.CancelledCannotCheckIn, result.Error);
    }

    [Fact]
    public void CheckIn_Twice_SecondIsRejected()
    {
        var harness = TestHarness.Create();
        var created = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));
        harness.RegistrationService.CheckIn(created.Value!.GuahaoId);

        var second = harness.RegistrationService.CheckIn(created.Value.GuahaoId);

        Assert.True(second.IsFailure);
        Assert.Equal(RegistrationMessages.AlreadyCheckedIn, second.Error);
    }

    [Fact]
    public void CheckIn_WhenCompleted_IsRejected()
    {
        var harness = TestHarness.Create();
        var created = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));
        var registration = harness.Registrations.GetById(created.Value!.GuahaoId)!;
        registration.CheckIn();
        registration.Complete();
        harness.Registrations.Update(registration);

        var result = harness.RegistrationService.CheckIn(created.Value.GuahaoId);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.CompletedCannotCheckIn, result.Error);
    }

    [Fact]
    public void Cancel_SetsStatusToCancelled()
    {
        var harness = TestHarness.Create();
        var created = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        var result = harness.RegistrationService.Cancel(created.Value!.GuahaoId);

        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatus.Cancelled, harness.Registrations.GetById(created.Value.GuahaoId)!.Status);
    }

    [Fact]
    public void Cancel_UnknownRegistration_ReturnsNotFound()
    {
        var harness = TestHarness.Create();

        var result = harness.RegistrationService.Cancel(98765);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.NotFound, result.Error);
    }

    // ---- 查詢 ----

    [Fact]
    public void Get_ReturnsDetail_WithLegacyShape()
    {
        var harness = TestHarness.Create();
        var created = harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        var detail = harness.RegistrationService.Get(created.Value!.GuahaoId);

        Assert.NotNull(detail);
        Assert.Equal(created.Value.GuahaoId, detail!.Id);
        Assert.Equal(Patient_張三, detail.PatientId);
        Assert.Equal(Session_內科早診, detail.ScheduleId);
        Assert.Equal(0, detail.Status); // 已掛號
        Assert.False(string.IsNullOrWhiteSpace(detail.CreateTime));
    }

    [Fact]
    public void Get_Unknown_ReturnsNull()
    {
        var harness = TestHarness.Create();

        Assert.Null(harness.RegistrationService.Get(55555));
    }

    [Fact]
    public void List_ReturnsMappedRows_AndHonoursFilters()
    {
        var harness = TestHarness.Create();
        var today = harness.Clock.Today.ToString("yyyy/MM/dd");
        harness.RegistrationService.Register(new CreateRegistrationRequest(Patient_張三, Session_內科早診));

        var all = harness.RegistrationService.List(null, null);
        var byDept = harness.RegistrationService.List(today, "INT");
        var otherDept = harness.RegistrationService.List(today, "SUR");

        var row = Assert.Single(all);
        Assert.Equal("張三", row.Patient);
        Assert.Equal("王大明", row.Doctor);
        Assert.Equal("INT", row.Dept);
        Assert.Equal("201", row.Room);
        Assert.Equal(0, row.Status);
        Assert.Equal("已掛號", row.StatusText);

        Assert.Single(byDept);
        Assert.Empty(otherDept);
    }
}
