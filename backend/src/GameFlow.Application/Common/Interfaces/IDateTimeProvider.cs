namespace GameFlow.Application.Common.Interfaces;

/// <summary>Test edilebilirlik için zamanı soyutlar.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly Today { get; }
}
