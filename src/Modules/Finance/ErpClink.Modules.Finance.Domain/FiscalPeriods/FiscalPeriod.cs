using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Finance.Domain.FiscalPeriods;

public sealed class FiscalPeriod : AggregateRoot
{
    private FiscalPeriod()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FiscalYearId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalPeriodStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static FiscalPeriod Create(
        Guid organizationId,
        Guid fiscalYearId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        string? createdBy,
        DateTime utcNow)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Fiscal period start date must be before end date.");

        var period = new FiscalPeriod
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FiscalYearId = fiscalYearId,
            Name = Require(name, 100),
            StartDate = startDate,
            EndDate = endDate,
            Status = FiscalPeriodStatus.Open
        };
        period.SetCreated(createdBy, utcNow);
        period.RaiseDomainEvent(new FiscalPeriodCreatedDomainEvent(period.Id, utcNow));
        return period;
    }

    public void Update(string name, DateOnly startDate, DateOnly endDate, string? updatedBy, DateTime utcNow)
    {
        EnsureOpen();
        if (startDate >= endDate)
            throw new ArgumentException("Fiscal period start date must be before end date.");

        Name = Require(name, 100);
        StartDate = startDate;
        EndDate = endDate;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new FiscalPeriodUpdatedDomainEvent(Id, utcNow));
    }

    public void Close(string? updatedBy, DateTime utcNow)
    {
        if (Status == FiscalPeriodStatus.Closed)
            throw new InvalidOperationException("Fiscal period is already closed.");

        Status = FiscalPeriodStatus.Closed;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new FiscalPeriodClosedDomainEvent(Id, utcNow));
    }

    public void EnsureOpenForPosting()
    {
        if (Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException("Journal posting requires an open fiscal period.");
    }

    public bool Contains(DateOnly journalDate) =>
        journalDate >= StartDate && journalDate <= EndDate;

    public bool Overlaps(DateOnly start, DateOnly end) =>
        start <= EndDate && end >= StartDate;

    private void EnsureOpen()
    {
        if (Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException("Closed fiscal periods cannot be modified.");
    }

    private static string Require(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public sealed class FiscalPeriodCreatedDomainEvent : IDomainEvent
{
    public FiscalPeriodCreatedDomainEvent(Guid fiscalPeriodId, DateTime occurredOnUtc) => (FiscalPeriodId, OccurredOnUtc) = (fiscalPeriodId, occurredOnUtc);
    public Guid FiscalPeriodId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class FiscalPeriodUpdatedDomainEvent : IDomainEvent
{
    public FiscalPeriodUpdatedDomainEvent(Guid fiscalPeriodId, DateTime occurredOnUtc) => (FiscalPeriodId, OccurredOnUtc) = (fiscalPeriodId, occurredOnUtc);
    public Guid FiscalPeriodId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class FiscalPeriodClosedDomainEvent : IDomainEvent
{
    public FiscalPeriodClosedDomainEvent(Guid fiscalPeriodId, DateTime occurredOnUtc) => (FiscalPeriodId, OccurredOnUtc) = (fiscalPeriodId, occurredOnUtc);
    public Guid FiscalPeriodId { get; }
    public DateTime OccurredOnUtc { get; }
}
