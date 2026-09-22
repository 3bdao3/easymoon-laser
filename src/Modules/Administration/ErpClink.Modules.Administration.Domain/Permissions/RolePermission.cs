namespace ErpClink.Modules.Administration.Domain.Permissions;

public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public Guid Id { get; private set; }
    public string RoleId { get; private set; } = string.Empty;
    public Guid PermissionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Permission? Permission { get; private set; }

    public static RolePermission Create(string roleId, Guid permissionId, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);

        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAtUtc = utcNow
        };
    }
}
