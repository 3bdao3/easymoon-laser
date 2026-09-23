using ErpClink.Modules.LaserClinic.Domain.Services;
using ErpClink.Modules.LaserClinic.Domain.Settings;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence;

public static class LaserClinicDbSeeder
{
    private static readonly (string Name, int Min, int Max, int Order, decimal Price)[] Catalog =
    [
        ("فيس ورقبة", 10, 10, 1, 200m),
        ("بكيني", 5, 5, 2, 150m),
        ("بكيني + لاين + أندر آرم", 15, 15, 3, 350m),
        ("نصف ذراع", 15, 15, 4, 200m),
        ("ذراع كامل", 25, 25, 5, 300m),
        ("نصف رجل", 20, 20, 6, 250m),
        ("نصف رجل علوي", 30, 30, 7, 300m),
        ("رجل كامل", 30, 45, 8, 450m),
        ("نصف جسم", 30, 40, 9, 500m),
        ("جسم كامل بدون بطن وظهر", 60, 60, 10, 700m),
        ("جسم كامل + بطن وظهر", 60, 75, 11, 900m),
        ("جسم كامل + بطن وظهر + فيس ورقبة", 75, 90, 12, 1100m),
        ("1000 نبضة", 15, 15, 13, 400m),
        ("5000 نبضة", 45, 45, 14, 1500m)
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
            foreach (var (name, min, max, order, price) in Catalog)
            {
                db.LaserServices.Add(LaserService.Create(name, min, max, order, price, null, "system", utc));
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
                db.LaserServices.Add(LaserService.Create(def.Name, def.Min, def.Max, def.Order, def.Price, null, "system", utcNow));
                changed = true;
                continue;
            }

            var price = match.Price > 0 ? match.Price : def.Price;
            if (match.MinDurationMinutes == def.Min
                && match.MaxDurationMinutes == def.Max
                && match.DisplayOrder == def.Order
                && match.Price == price)
                continue;

            match.Update(def.Name, def.Min, def.Max, def.Order, price, match.Notes, "system", utcNow);
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Synced laser service catalog (including pulse packages).");
        }
    }
}
