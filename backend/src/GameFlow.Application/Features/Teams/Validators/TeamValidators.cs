using FluentValidation;
using GameFlow.Application.Common.Validation;
using GameFlow.Application.Features.Teams.Dtos;

namespace GameFlow.Application.Features.Teams.Validators;

/// <summary>Takım isteklerinde tekrar eden kurallar.</summary>
internal static class TeamRules
{
    public static IRuleBuilderOptions<T, string> TeamName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Takım adı zorunludur.")
            .MinimumLength(2).WithMessage("Takım adı en az 2 karakter olmalıdır.")
            .MaximumLength(96).WithMessage("Takım adı en fazla 96 karakter olabilir.");

    /// <summary>#RGB, #RRGGBB veya #RRGGBBAA biçimini kabul eder.</summary>
    public static IRuleBuilderOptions<T, string> HexColor<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Renk zorunludur.")
            .Matches("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
            .WithMessage("Renk #RRGGBB biçiminde olmalıdır.");
}

public class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamRequestValidator()
    {
        RuleFor(x => x.Name).TeamName();
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.Category).ValidEnum();
        RuleFor(x => x.ColorHex).HexColor();
        RuleFor(x => x.IconKey).MaximumLength(64);
        RuleFor(x => x.MemberIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı kullanıcı birden fazla kez seçilemez.");
    }
}

public class UpdateTeamRequestValidator : AbstractValidator<UpdateTeamRequest>
{
    public UpdateTeamRequestValidator()
    {
        RuleFor(x => x.Name).TeamName();
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.Category).ValidEnum();
        RuleFor(x => x.ColorHex).HexColor();
        RuleFor(x => x.IconKey).MaximumLength(64);
    }
}

public class AddTeamMembersRequestValidator : AbstractValidator<AddTeamMembersRequest>
{
    public AddTeamMembersRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("En az bir kullanıcı seçmelisiniz.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı kullanıcı birden fazla kez seçilemez.");
    }
}
