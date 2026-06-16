using HisModern.Domain.Entities;
using HisModern.Domain.Enums;

namespace HisModern.Tests.Domain;

/// <summary>
/// 商業規則 6：報到只允許從「已掛號」；已取消 / 已完成不可報到；取消即設為已取消。
/// </summary>
public sealed class RegistrationTests
{
    private static Registration NewRegistration()
        => Registration.Create(1000, patientId: 1, clinicSessionId: 1, number: 1, createdAt: DateTime.Now);

    [Fact]
    public void Create_StartsAsRegistered_AndActive()
    {
        var registration = NewRegistration();

        Assert.Equal(RegistrationStatus.Registered, registration.Status);
        Assert.True(registration.IsActive);
    }

    [Fact]
    public void CheckIn_FromRegistered_Succeeds()
    {
        var registration = NewRegistration();

        var error = registration.CheckIn();

        Assert.Null(error);
        Assert.Equal(RegistrationStatus.CheckedIn, registration.Status);
    }

    [Fact]
    public void CheckIn_WhenCancelled_IsRejected()
    {
        var registration = NewRegistration();
        registration.Cancel();

        var error = registration.CheckIn();

        Assert.Equal(RegistrationTransitionError.CancelledCannotCheckIn, error);
        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
    }

    [Fact]
    public void CheckIn_WhenCompleted_IsRejected()
    {
        var registration = NewRegistration();
        registration.CheckIn();
        Assert.True(registration.Complete());

        var error = registration.CheckIn();

        Assert.Equal(RegistrationTransitionError.CompletedCannotCheckIn, error);
        Assert.Equal(RegistrationStatus.Completed, registration.Status);
    }

    [Fact]
    public void CheckIn_WhenAlreadyCheckedIn_IsRejected()
    {
        var registration = NewRegistration();
        registration.CheckIn();

        var error = registration.CheckIn();

        Assert.Equal(RegistrationTransitionError.AlreadyCheckedIn, error);
        Assert.Equal(RegistrationStatus.CheckedIn, registration.Status);
    }

    [Theory]
    [InlineData(RegistrationStatus.Registered)]
    [InlineData(RegistrationStatus.CheckedIn)]
    [InlineData(RegistrationStatus.Completed)]
    public void Cancel_FromAnyState_SetsCancelled(RegistrationStatus startStatus)
    {
        var registration = Registration.Restore(
            1000, patientId: 1, clinicSessionId: 1, number: 1, status: startStatus, createdAt: DateTime.Now);

        registration.Cancel();

        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
        Assert.False(registration.IsActive);
    }

    [Fact]
    public void Complete_OnlyAllowedFromCheckedIn()
    {
        var registration = NewRegistration();

        // 尚未報到，不可完成。
        Assert.False(registration.Complete());

        registration.CheckIn();
        Assert.True(registration.Complete());
        Assert.Equal(RegistrationStatus.Completed, registration.Status);
    }
}
