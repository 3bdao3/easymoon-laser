using ErpClink.BuildingBlocks.Application.Abstractions;

namespace ErpClink.BuildingBlocks.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
