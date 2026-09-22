using ErpClink.Modules.Administration.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Administration.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.NormalizedName).IsUnique().HasFilter("[NormalizedName] IS NOT NULL");
    }
}
