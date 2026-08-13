using FluentValidation;
using GameFlow.Application.Common.Validation;
using GameFlow.Application.Features.WorkItems.Dtos;

namespace GameFlow.Application.Features.WorkItems.Validators;

internal static class WorkItemRules
{
    public static IRuleBuilderOptions<T, string> WorkItemTitle<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Görev başlığı zorunludur.")
            .MinimumLength(3).WithMessage("Görev başlığı en az 3 karakter olmalıdır.")
            .MaximumLength(256).WithMessage("Görev başlığı en fazla 256 karakter olabilir.");

    public static IRuleBuilderOptions<T, decimal?> WorkHours<T>(this IRuleBuilder<T, decimal?> rule)
        => rule
            .InclusiveBetween(0m, 9999m)
            .When(value => value is not null)
            .WithMessage("Süre 0 ile 9999 saat arasında olmalıdır.");

    public static IRuleBuilderOptions<T, int?> Points<T>(this IRuleBuilder<T, int?> rule)
        => rule
            .InclusiveBetween(0, 1000)
            .When(value => value is not null)
            .WithMessage("Puan 0 ile 1000 arasında olmalıdır.");
}

public class CreateWorkItemRequestValidator : AbstractValidator<CreateWorkItemRequest>
{
    public CreateWorkItemRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Proje seçilmelidir.");
        RuleFor(x => x.Title).WorkItemTitle();
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.Status).ValidEnum();
        RuleFor(x => x.Priority).ValidEnum();
        RuleFor(x => x.Type).ValidEnum();
        RuleFor(x => x.EstimatedHours).WorkHours();
        RuleFor(x => x.StoryPoints).Points();

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Son teslim tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(x => x.LabelIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı etiket birden fazla kez seçilemez.");

        RuleForEach(x => x.ChecklistItems)
            .MaximumLength(512).WithMessage("Kontrol listesi maddesi en fazla 512 karakter olabilir.");
    }
}

public class UpdateWorkItemRequestValidator : AbstractValidator<UpdateWorkItemRequest>
{
    public UpdateWorkItemRequestValidator()
    {
        RuleFor(x => x.Title).WorkItemTitle();
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.Priority).ValidEnum();
        RuleFor(x => x.Type).ValidEnum();
        RuleFor(x => x.EstimatedHours).WorkHours();
        RuleFor(x => x.LoggedHours).WorkHours();
        RuleFor(x => x.StoryPoints).Points();

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Son teslim tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(x => x.LabelIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı etiket birden fazla kez seçilemez.");
    }
}

public class MoveWorkItemRequestValidator : AbstractValidator<MoveWorkItemRequest>
{
    public MoveWorkItemRequestValidator()
    {
        RuleFor(x => x.TargetStatus).ValidEnum();

        RuleFor(x => x)
            .Must(x => x.PrecedingItemId != x.FollowingItemId || x.PrecedingItemId is null)
            .WithMessage("Kartın üstündeki ve altındaki görev aynı olamaz.");
    }
}

public class ChangeStatusRequestValidator : AbstractValidator<ChangeStatusRequest>
{
    public ChangeStatusRequestValidator() => RuleFor(x => x.Status).ValidEnum();
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
        => RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum boş olamaz.")
            .MaximumLength(4000).WithMessage("Yorum en fazla 4000 karakter olabilir.");
}

public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
        => RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum boş olamaz.")
            .MaximumLength(4000).WithMessage("Yorum en fazla 4000 karakter olabilir.");
}

public class CreateChecklistItemRequestValidator : AbstractValidator<CreateChecklistItemRequest>
{
    public CreateChecklistItemRequestValidator()
        => RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Madde metni zorunludur.")
            .MaximumLength(512).WithMessage("Madde en fazla 512 karakter olabilir.");
}

public class UpdateChecklistItemRequestValidator : AbstractValidator<UpdateChecklistItemRequest>
{
    public UpdateChecklistItemRequestValidator()
        => RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Madde metni zorunludur.")
            .MaximumLength(512).WithMessage("Madde en fazla 512 karakter olabilir.");
}

public class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Etiket adı zorunludur.")
            .MaximumLength(48).WithMessage("Etiket adı en fazla 48 karakter olabilir.");

        RuleFor(x => x.ColorHex)
            .Matches("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
            .WithMessage("Renk #RRGGBB biçiminde olmalıdır.");
    }
}
