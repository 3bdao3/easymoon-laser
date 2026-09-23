using ErpClink.Modules.LaserClinic.Domain.Appointments;
using ErpClink.Modules.LaserClinic.Domain.Customers;
using ErpClink.Modules.LaserClinic.Domain.Offers;
using ErpClink.Modules.LaserClinic.Domain.Services;
using ErpClink.Modules.LaserClinic.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence;

public sealed class LaserClinicDbContext : DbContext
{
    public LaserClinicDbContext(DbContextOptions<LaserClinicDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LaserService> LaserServices => Set<LaserService>();
    public DbSet<LaserOffer> LaserOffers => Set<LaserOffer>();
    public DbSet<LaserAppointment> Appointments => Set<LaserAppointment>();
    public DbSet<AppointmentServiceLine> AppointmentServices => Set<AppointmentServiceLine>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("laser");

        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("Customers");
            b.HasKey(x => x.Id);
            b.Ignore(x => x.DomainEvents);
            b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            b.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.PackagePriceTotal).HasPrecision(18, 2);
            b.HasIndex(x => x.PhoneNumber);
            b.HasIndex(x => x.FullName);
        });

        modelBuilder.Entity<LaserService>(b =>
        {
            b.ToTable("LaserServices");
            b.HasKey(x => x.Id);
            b.Ignore(x => x.DomainEvents);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.HasIndex(x => x.DisplayOrder);
        });

        modelBuilder.Entity<LaserOffer>(b =>
        {
            b.ToTable("LaserOffers");
            b.HasKey(x => x.Id);
            b.Ignore(x => x.DomainEvents);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.HasIndex(x => x.DisplayOrder);
            b.HasIndex(x => x.IsActive);
        });

        modelBuilder.Entity<LaserAppointment>(b =>
        {
            b.ToTable("Appointments");
            b.HasKey(x => x.Id);
            b.Ignore(x => x.DomainEvents);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.AmountPaid).HasPrecision(18, 2);
            b.HasIndex(x => x.AppointmentDate);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => new { x.AppointmentDate, x.StartTime });
            b.HasMany(x => x.Services)
                .WithOne()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Services).HasField("_services");
        });

        modelBuilder.Entity<AppointmentServiceLine>(b =>
        {
            b.ToTable("AppointmentServices");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.AppointmentId);
            b.HasIndex(x => x.LaserServiceId);
            b.HasOne<LaserService>()
                .WithMany()
                .HasForeignKey(x => x.LaserServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicSettings>(b =>
        {
            b.ToTable("ClinicSettings");
            b.HasKey(x => x.Id);
            b.Ignore(x => x.DomainEvents);
        });
    }
}
