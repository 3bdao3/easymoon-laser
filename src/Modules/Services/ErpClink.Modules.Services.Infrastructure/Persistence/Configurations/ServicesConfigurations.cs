using ErpClink.Modules.Services.Domain.Catalog;
using ErpClink.Modules.Services.Domain.Categories;
using ErpClink.Modules.Services.Domain.Packages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Services.Infrastructure.Persistence.Configurations;

public sealed class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class HealthcareServiceConfiguration : IEntityTypeConfiguration<HealthcareService>
{
    public void Configure(EntityTypeBuilder<HealthcareService> builder)
    {
        builder.ToTable("HealthcareServices");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.ServiceCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.DefaultPrice).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Prices)
            .WithOne()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Prices).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.OrganizationId, x.ServiceCode }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.CategoryId });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class ServicePriceConfiguration : IEntityTypeConfiguration<ServicePrice>
{
    public void Configure(EntityTypeBuilder<ServicePrice> builder)
    {
        builder.ToTable("ServicePrices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => new { x.ServiceId, x.EffectiveToUtc });
    }
}

public sealed class HealthcarePackageConfiguration : IEntityTypeConfiguration<HealthcarePackage>
{
    public void Configure(EntityTypeBuilder<HealthcarePackage> builder)
    {
        builder.ToTable("HealthcarePackages");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.PackageCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.OrganizationId, x.PackageCode }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        builder.ToTable("PackageItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 2);
        builder.HasIndex(x => x.PackageId);
        builder.HasIndex(x => new { x.PackageId, x.ServiceId }).IsUnique();
    }
}
