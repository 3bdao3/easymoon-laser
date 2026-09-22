namespace ErpClink.BuildingBlocks.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
