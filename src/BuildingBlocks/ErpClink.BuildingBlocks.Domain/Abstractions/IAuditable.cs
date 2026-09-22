namespace ErpClink.BuildingBlocks.Domain.Abstractions;

public interface IAuditable
{
    DateTime CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTime? UpdatedAtUtc { get; }
    string? UpdatedBy { get; }

    void SetCreated(string? userId, DateTime utcNow);
    void SetUpdated(string? userId, DateTime utcNow);
}
