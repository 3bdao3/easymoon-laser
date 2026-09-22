namespace ErpClink.Modules.Procurement.Domain;

/// <summary>Authoritative monetary rounding for Procurement (2 decimal places).</summary>
public static class Money
{
    public const int Scale = 2;

    public static decimal Round(decimal value) =>
        Math.Round(value, Scale, MidpointRounding.AwayFromZero);

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
