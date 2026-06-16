using FluentValidation;
using HisModern.Application.Contracts;
using HisModern.Application.Services;

namespace HisModern.Application.Validation;

/// <summary>
/// 新增掛號請求的輸入驗證。取代 legacy 在 Controller 內以 if 逐行檢查的做法。
/// 規則宣告順序刻意對齊 legacy (先 patientId 後 scheduleId)，以維持錯誤訊息一致。
/// </summary>
public sealed class CreateRegistrationRequestValidator : AbstractValidator<CreateRegistrationRequest>
{
    public CreateRegistrationRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .GreaterThan(0)
            .WithMessage(RegistrationMessages.PatientIdRequired);

        RuleFor(x => x.ScheduleId)
            .GreaterThan(0)
            .WithMessage(RegistrationMessages.ScheduleIdRequired);
    }
}
