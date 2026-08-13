using FluentValidation;
using GameFlow.Application.Features.Sprints.Dtos;

namespace GameFlow.Application.Features.Sprints.Validators;

internal static class SprintRules
{
    /// <summary>Sprint süresi en az 1, en fazla 60 gün olabilir.</summary>
    private const int MaxSprintDays = 60;

    public static IRuleBuilderOptions<T, string> SprintName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Sprint adı zorunludur.")
            .MinimumLength(2).WithMessage("Sprint adı en az 2 karakter olmalıdır.")
            .MaximumLength(128).WithMessage("Sprint adı en fazla 128 karakter olabilir.");

    public static bool IsValidDuration(DateTime startDate, DateTime endDate)
    {
        var days = (endDate.Date - startDate.Date).TotalDays;
        return days >= 1 && days <= MaxSprintDays;
    }
}

public class CreateSprintRequestValidator : AbstractValidator<CreateSprintRequest>
{
    public CreateSprintRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje seçilmelidir.");
        RuleFor(x => x.Name).SprintName();
        RuleFor(x => x.Goal).MaximumLength(1024);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("Sprint bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        RuleFor(x => x)
            .Must(x => SprintRules.IsValidDuration(x.StartDate, x.EndDate))
            .When(x => x.EndDate > x.StartDate)
            .WithMessage("Sprint süresi 1 ile 60 gün arasında olmalıdır.");
    }
}

public class UpdateSprintRequestValidator : AbstractValidator<UpdateSprintRequest>
{
    public UpdateSprintRequestValidator()
    {
        RuleFor(x => x.Name).SprintName();
        RuleFor(x => x.Goal).MaximumLength(1024);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("Sprint bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        RuleFor(x => x)
            .Must(x => SprintRules.IsValidDuration(x.StartDate, x.EndDate))
            .When(x => x.EndDate > x.StartDate)
            .WithMessage("Sprint süresi 1 ile 60 gün arasında olmalıdır.");
    }
}

public class CompleteSprintRequestValidator : AbstractValidator<CompleteSprintRequest>
{
    public CompleteSprintRequestValidator()
        => RuleFor(x => x.RetrospectiveNotes).MaximumLength(4000);
}
