using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameFlow.Infrastructure.Persistence;

/// <summary>
/// Veritabanına yazarken DateTime değerini UTC'ye çevirir, okurken Kind bilgisini UTC olarak işaretler.
/// Npgsql "timestamp with time zone" sütunları için gereklidir.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}

/// <inheritdoc cref="UtcDateTimeConverter"/>
public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            value => value.HasValue
                ? (value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime())
                : value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value)
    {
    }
}
