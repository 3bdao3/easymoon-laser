using ErpClink.Modules.LaserClinic.Domain.Services;
using ErpClink.Modules.LaserClinic.Domain.Settings;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence;

public static class LaserClinicDbSeeder
{
    private static readonly (string Name, int Min, int Max, int Order)[] Catalog =
    [
        ("فيس ورقبة", 10, 10, 1),
        ("بكيني", 5, 5, 2),
        ("بكيني + لاين + أندر آرم", 15, 15, 3),
        ("نصف ذراع", 15, 15, 4),
        ("ذراع كامل", 25, 25, 5),
        ("نصف رجل", 20, 20, 6),
        ("نصف رجل علوي", 30, 30, 7),
        ("رجل كامل", 30, 45, 8),
        ("نصف جسم", 30, 40, 9),
        ("جسم كامل بدون بطن وظهر", 60, 60, 10),
        ("جسم كامل + بطن وظهر", 60, 75, 11),
        ("جسم كامل + بطن وظهر + فيس ورقبة", 75, 90, 12),
        ("1000 نبضة", 15, 15, 13),
        ("5000 نبضة", 45, 45, 14)
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LaserClinicDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("LaserClinicDbSeeder");

        if (!await db.ClinicSettings.AnyAsync(cancellationToken))
        {
            db.ClinicSettings.Add(ClinicSettings.CreateDefault("system", DateTime.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded default clinic settings (16:00–22:00).");
        }

        if (!await db.LaserServices.AnyAsync(cancellationToken))
        {
            var utc = DateTime.UtcNow;
            foreach (var (name, min, max, order) in Catalog)
            {
                db.LaserServices.Add(LaserService.Create(name, min, max, order, null, "system", utc));
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} laser services.", Catalog.Length);
            return;
        }

        // Sync catalog: add missing services and align durations/order for existing ones by name.
        var existing = await db.LaserServices.ToListAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var changed = false;
        foreach (var def in Catalog)
        {
            var match = existing.FirstOrDefault(s => s.Name == def.Name);
            if (match is null)
            {
                db.LaserServices.Add(LaserService.Create(def.Name, def.Min, def.Max, def.Order, null, "system", utcNow));
                changed = true;
                continue;
            }

            if (match.MinDurationMinutes == def.Min
                && match.MaxDurationMinutes == def.Max
                && match.DisplayOrder == def.Order)
                continue;

            match.Update(def.Name, def.Min, def.Max, def.Order, match.Notes, "system", utcNow);
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Synced laser service catalog (including pulse packages).");
        }
    }
}
