using FluentValidation;
using GameFlow.Application.Common.Validation;
using GameFlow.Application.Features.Announcements.Dtos;

namespace GameFlow.Application.Features.Announcements.Validators;

public class CreateAnnouncementRequestValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Duyuru başlığı zorunludur.")
            .MaximumLength(192).WithMessage("Başlık en fazla 192 karakter olabilir.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Duyuru içeriği zorunludur.")
            .MaximumLength(8000).WithMessage("İçerik en fazla 8000 karakter olabilir.");

        RuleFor(x => x.Priority).ValidEnum();
    }
}

public class UpdateAnnouncementRequestValidator : AbstractValidator<UpdateAnnouncementRequest>
{
    public UpdateAnnouncementRequestValidator()
        => Include(new CreateAnnouncementRequestValidator());
}
