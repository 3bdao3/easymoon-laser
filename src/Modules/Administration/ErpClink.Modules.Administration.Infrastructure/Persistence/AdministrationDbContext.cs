using ErpClink.Modules.Administration.Domain.Auth;
using ErpClink.Modules.Administration.Domain.Permissions;
using ErpClink.Modules.Administration.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Administration.Infrastructure.Persistence;

public sealed class AdministrationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AdministrationDbContext(DbContextOptions<AdministrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("admin");
        builder.ApplyConfigurationsFromAssembly(typeof(AdministrationDbContext).Assembly);
    }
}
