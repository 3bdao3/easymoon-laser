namespace ErpClink.Modules.Billing.Domain.Payments;

public enum PaymentStatus
{
    Captured = 0,
    Reversed = 1
}

/// <summary>
/// Controlled payment methods for V1 manual capture.
/// Exact clinic list is an unresolved stakeholder decision; these values are provisional.
/// </summary>
public enum PaymentMethod
{
    Cash = 0,
    Card = 1,
    BankTransfer = 2,
    Other = 3
}
