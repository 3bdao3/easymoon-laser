using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Administration.Domain.Permissions;

public sealed class Permission : AuditableEntity
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Permission Create(
        string code,
        string name,
        string module,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Module = module.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };

        permission.SetCreated(createdBy, utcNow);
        return permission;
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
    }
}
