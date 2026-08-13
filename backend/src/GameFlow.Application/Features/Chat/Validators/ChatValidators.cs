using FluentValidation;
using GameFlow.Application.Features.Chat.Dtos;

namespace GameFlow.Application.Features.Chat.Validators;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
        => RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Mesaj boş olamaz.")
            .MaximumLength(4000).WithMessage("Mesaj en fazla 4000 karakter olabilir.");
}

public class EditMessageRequestValidator : AbstractValidator<EditMessageRequest>
{
    public EditMessageRequestValidator()
        => RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Mesaj boş olamaz.")
            .MaximumLength(4000).WithMessage("Mesaj en fazla 4000 karakter olabilir.");
}
