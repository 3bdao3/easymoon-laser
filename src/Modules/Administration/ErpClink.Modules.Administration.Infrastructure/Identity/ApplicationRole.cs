using Microsoft.AspNetCore.Identity;

namespace ErpClink.Modules.Administration.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
