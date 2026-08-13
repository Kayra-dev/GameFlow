using FluentValidation;
using GameFlow.Application.Common.Validation;
using GameFlow.Application.Features.Projects.Dtos;

namespace GameFlow.Application.Features.Projects.Validators;

internal static class ProjectRules
{
    public static IRuleBuilderOptions<T, string> ProjectName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Proje adı zorunludur.")
            .MinimumLength(2).WithMessage("Proje adı en az 2 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Proje adı en fazla 128 karakter olabilir.");

    public static IRuleBuilderOptions<T, string> HexColor<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Renk zorunludur.")
            .Matches("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
            .WithMessage("Renk #RRGGBB biçiminde olmalıdır.");
}

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).ProjectName();

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Proje anahtarı zorunludur.")
            .Matches("^[A-Za-z][A-Za-z0-9]{1,9}$")
            .WithMessage("Proje anahtarı harfle başlamalı, 2-10 karakter olmalı ve yalnızca harf/rakam içermelidir.");

        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Status).ValidEnum();
        RuleFor(x => x.ColorHex).HexColor();
        RuleFor(x => x.Genre).MaximumLength(64);
        RuleFor(x => x.Platforms).MaximumLength(256);

        RuleFor(x => x.TargetReleaseDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.TargetReleaseDate.HasValue)
            .WithMessage("Hedef çıkış tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(x => x.MemberIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı kullanıcı birden fazla kez seçilemez.");
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).ProjectName();
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Status).ValidEnum();
        RuleFor(x => x.ColorHex).HexColor();
        RuleFor(x => x.Genre).MaximumLength(64);
        RuleFor(x => x.Platforms).MaximumLength(256);

        RuleFor(x => x.TargetReleaseDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.TargetReleaseDate.HasValue)
            .WithMessage("Hedef çıkış tarihi başlangıç tarihinden önce olamaz.");
    }
}

public class AddProjectMembersRequestValidator : AbstractValidator<AddProjectMembersRequest>
{
    public AddProjectMembersRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("En az bir kullanıcı seçmelisiniz.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı kullanıcı birden fazla kez seçilemez.");
    }
}
