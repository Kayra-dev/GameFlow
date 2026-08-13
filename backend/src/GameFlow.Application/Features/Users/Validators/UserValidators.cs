using FluentValidation;
using GameFlow.Application.Common.Validation;
using GameFlow.Application.Features.Users.Dtos;

namespace GameFlow.Application.Features.Users.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).PersonName();
        RuleFor(x => x.Email).EmailAddressRule();
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.Role).ValidEnum();
        RuleFor(x => x.JobTitle).MaximumLength(128);
        RuleFor(x => x.Bio).MaximumLength(1024);
        RuleFor(x => x.TeamIds).Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı takım birden fazla kez seçilemez.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).PersonName();
        RuleFor(x => x.Role).ValidEnum();
        RuleFor(x => x.JobTitle).MaximumLength(128);
        RuleFor(x => x.Bio).MaximumLength(1024);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).Password();
    }
}
