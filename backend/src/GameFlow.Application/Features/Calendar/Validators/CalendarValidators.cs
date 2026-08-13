using FluentValidation;
using GameFlow.Application.Common.Validation;
using GameFlow.Application.Features.Calendar.Dtos;

namespace GameFlow.Application.Features.Calendar.Validators;

public class CalendarRangeRequestValidator : AbstractValidator<CalendarRangeRequest>
{
    /// <summary>Tek sorguda getirilebilecek en geniş aralık.</summary>
    private const int MaxRangeDays = 400;

    public CalendarRangeRequestValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.");

        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .WithMessage("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= MaxRangeDays)
            .When(x => x.To > x.From)
            .WithMessage($"Takvim aralığı en fazla {MaxRangeDays} gün olabilir.");
    }
}

public class CreateCalendarEventRequestValidator : AbstractValidator<CreateCalendarEventRequest>
{
    public CreateCalendarEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Etkinlik başlığı zorunludur.")
            .MaximumLength(192).WithMessage("Başlık en fazla 192 karakter olabilir.");

        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Type).ValidEnum();
        RuleFor(x => x.StartsAt).NotEmpty().WithMessage("Başlangıç zamanı zorunludur.");

        RuleFor(x => x.ColorHex)
            .Matches("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
            .WithMessage("Renk #RRGGBB biçiminde olmalıdır.");

        RuleFor(x => x.EndsAt)
            .GreaterThanOrEqualTo(x => x.StartsAt)
            .When(x => x.EndsAt.HasValue)
            .WithMessage("Bitiş zamanı başlangıçtan önce olamaz.");
    }
}

public class UpdateCalendarEventRequestValidator : AbstractValidator<UpdateCalendarEventRequest>
{
    public UpdateCalendarEventRequestValidator()
        => Include(new CreateCalendarEventRequestValidator());
}

public class CreateMeetingRequestValidator : AbstractValidator<CreateMeetingRequest>
{
    /// <summary>Tek bir toplantı en fazla 24 saat sürebilir.</summary>
    private const int MaxDurationHours = 24;

    public CreateMeetingRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Toplantı başlığı zorunludur.")
            .MaximumLength(192).WithMessage("Başlık en fazla 192 karakter olabilir.");

        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(192);

        RuleFor(x => x.MeetingUrl)
            .MaximumLength(512)
            .Must(url => string.IsNullOrWhiteSpace(url)
                         || Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Toplantı bağlantısı geçerli bir http/https adresi olmalıdır.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Toplantı bitişi başlangıçtan sonra olmalıdır.");

        RuleFor(x => x)
            .Must(x => (x.EndsAt - x.StartsAt).TotalHours <= MaxDurationHours)
            .When(x => x.EndsAt > x.StartsAt)
            .WithMessage($"Toplantı süresi en fazla {MaxDurationHours} saat olabilir.");

        RuleFor(x => x.AttendeeIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı katılımcı birden fazla kez seçilemez.");
    }
}

public class UpdateMeetingRequestValidator : AbstractValidator<UpdateMeetingRequest>
{
    public UpdateMeetingRequestValidator()
    {
        Include(new CreateMeetingRequestValidator());
        RuleFor(x => x.Status).ValidEnum();
    }
}
