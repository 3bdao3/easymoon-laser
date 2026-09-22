namespace ErpClink.Modules.Inventory.Domain;

/// <summary>
/// Inventory costing precision. Unit costs keep 6 dp; financial totals round to 2 dp (Money.Scale).
/// </summary>
public static class InventoryCost
{
    public const int UnitCostScale = 6;

    public static decimal RoundUnitCost(decimal value) =>
        Math.Round(value, UnitCostScale, MidpointRounding.AwayFromZero);

    public static decimal RoundMoney(decimal value) => Money.Round(value);

    public static decimal CalculateTotal(decimal quantity, decimal unitCost) =>
        RoundMoney(Quantity.Round(quantity) * RoundUnitCost(unitCost));

    public static void EnsureNonNegativeUnitCost(decimal unitCost, string name)
    {
        if (unitCost < 0)
            throw new ArgumentOutOfRangeException(name, "Unit cost cannot be negative.");
    }
}
