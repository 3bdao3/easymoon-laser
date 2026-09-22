namespace ErpClink.BuildingBlocks.Domain.Abstractions;

public abstract class AuditableEntity : IAuditable
{
    public DateTime CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime? UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    public void SetCreated(string? userId, DateTime utcNow)
    {
        CreatedAtUtc = utcNow;
        CreatedBy = userId;
    }

    public void SetUpdated(string? userId, DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        UpdatedBy = userId;
    }
}
