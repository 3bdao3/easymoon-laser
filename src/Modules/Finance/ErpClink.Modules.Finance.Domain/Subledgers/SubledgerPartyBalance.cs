namespace ErpClink.Modules.Finance.Domain.Subledgers;

/// <summary>
/// Materialized party balance derived only from posted subledger transactions (including reversed originals + compensations via signed sum of all rows).
/// Not manually editable.
/// </summary>
public sealed class SubledgerPartyBalance
{
    private SubledgerPartyBalance()
    {
    }

    public Guid OrganizationId { get; private set; }
    public SubledgerType SubledgerType { get; private set; }
    public Guid PartyId { get; private set; }
    public decimal Balance { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public DateTime UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static SubledgerPartyBalance Create(
        Guid organizationId,
        SubledgerType subledgerType,
        Guid partyId,
        string currencyCode,
        DateTime utcNow)
    {
        return new SubledgerPartyBalance
        {
            OrganizationId = organizationId,
            SubledgerType = subledgerType,
            PartyId = partyId,
            Balance = 0m,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            UpdatedAtUtc = utcNow
        };
    }

    public void ApplyImpact(decimal signedImpact, DateTime utcNow)
    {
        Balance = Money.Round(Balance + signedImpact);
        UpdatedAtUtc = utcNow;
    }
}
