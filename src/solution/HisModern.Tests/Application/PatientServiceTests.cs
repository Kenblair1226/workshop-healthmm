using HisModern.Application.Services;
using HisModern.Tests.Common;

namespace HisModern.Tests.Application;

/// <summary>商業規則 1 (年齡) 透過服務層的驗證。</summary>
public sealed class PatientServiceTests
{
    [Fact]
    public void GetAge_ForAdult_ReturnsFullYears()
    {
        var harness = TestHarness.Create(); // today = 2026/06/15

        var result = harness.PatientService.GetAge(1); // 張三 1985/03/12

        Assert.True(result.IsSuccess);
        Assert.Equal("張三", result.Value!.Name);
        Assert.Equal(41, result.Value.Age);
    }

    [Fact]
    public void GetAge_BeforeBirthdayThisYear_IsOneLess()
    {
        var harness = TestHarness.Create(); // today = 2026/06/15

        var result = harness.PatientService.GetAge(2); // 李四 1990-07-08 (生日尚未到)

        Assert.True(result.IsSuccess);
        Assert.Equal(35, result.Value!.Age);
    }

    [Fact]
    public void GetAge_ForChild_ParsesDashSeparatedSeed()
    {
        var harness = TestHarness.Create();

        var result = harness.PatientService.GetAge(3); // 小明 2020/01/20

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value!.Age);
    }

    [Fact]
    public void GetAge_UnknownPatient_Fails()
    {
        var harness = TestHarness.Create();

        var result = harness.PatientService.GetAge(404);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationMessages.PatientNotFound, result.Error);
    }
}
