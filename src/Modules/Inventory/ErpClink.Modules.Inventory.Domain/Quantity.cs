namespace ErpClink.Modules.Inventory.Domain;

public static class Quantity
{
    public const int Scale = 3;

    public static decimal Round(decimal value) => Math.Round(value, Scale, MidpointRounding.AwayFromZero);

    public static void EnsureNonNegative(decimal value, string name)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
    }

    public static void EnsurePositive(decimal value, string name)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(name, $"{name} must be greater than zero.");
    }
}
