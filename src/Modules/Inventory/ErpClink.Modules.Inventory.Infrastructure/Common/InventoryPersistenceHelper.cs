using ErpClink.BuildingBlocks.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Common;

internal static class InventoryPersistenceHelper
{
    public static async Task SaveChangesAsync(DbContext db, string concurrencyCode, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(concurrencyCode, "The record was modified by another operation.", 409);
        }
    }

    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    public static void ApplyRowVersion<T>(DbContext db, T entity, byte[]? rowVersion, System.Linq.Expressions.Expression<Func<T, byte[]>> property)
        where T : class
    {
        if (rowVersion is { Length: > 0 })
            db.Entry(entity).Property(property).OriginalValue = rowVersion;
    }
}
