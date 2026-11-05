using Application.Common.Interfaces;

namespace Infrastructure.DateTimeProvider;

public class SystemDateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}