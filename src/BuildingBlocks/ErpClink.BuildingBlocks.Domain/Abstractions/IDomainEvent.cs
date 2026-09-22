namespace ErpClink.BuildingBlocks.Domain.Abstractions;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
