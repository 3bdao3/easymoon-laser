namespace ErpClink.Modules.Finance.Domain.Journals;

public enum JournalStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}

public enum JournalHistoryEventType
{
    Created = 1,
    Posted = 2,
    Reversed = 3
}
