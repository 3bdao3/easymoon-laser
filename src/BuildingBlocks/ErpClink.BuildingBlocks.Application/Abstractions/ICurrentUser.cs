namespace ErpClink.BuildingBlocks.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool HasPermission(string permissionCode);
}
