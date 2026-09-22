namespace ErpClink.Modules.Finance.Domain.Subledgers;

public enum SubledgerType
{
    Ar = 0,
    Ap = 1
}

public enum SubledgerDirection
{
    /// <summary>AR: increases receivable. AP: decreases payable.</summary>
    Debit = 0,
    /// <summary>AR: decreases receivable. AP: increases payable.</summary>
    Credit = 1
}

public enum SubledgerTransactionStatus
{
    Posted = 0,
    Reversed = 1
}
