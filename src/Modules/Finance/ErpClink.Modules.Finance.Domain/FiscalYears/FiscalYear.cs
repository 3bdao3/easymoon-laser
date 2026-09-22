using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Finance.Domain.FiscalYears;

public sealed class FiscalYear : AggregateRoot
{
    private FiscalYear()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public FiscalYearStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static FiscalYear Create(
        Guid organizationId,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        string? createdBy,
        DateTime utcNow)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Fiscal year start date must be before end date.");

        var year = new FiscalYear
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = Require(name, 100),
            StartDate = startDate,
            EndDate = endDate,
            Status = FiscalYearStatus.Open
        };
        year.SetCreated(createdBy, utcNow);
        year.RaiseDomainEvent(new FiscalYearCreatedDomainEvent(year.Id, utcNow));
        return year;
    }

    public void Update(string name, DateOnly startDate, DateOnly endDate, string? updatedBy, DateTime utcNow)
    {
        EnsureOpen();
        if (startDate >= endDate)
            throw new ArgumentException("Fiscal year start date must be before end date.");

        Name = Require(name, 100);
        StartDate = startDate;
        EndDate = endDate;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new FiscalYearUpdatedDomainEvent(Id, utcNow));
    }

    public void Close(string? updatedBy, DateTime utcNow)
    {
        if (Status == FiscalYearStatus.Closed)
            throw new InvalidOperationException("Fiscal year is already closed.");

        Status = FiscalYearStatus.Closed;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new FiscalYearClosedDomainEvent(Id, utcNow));
    }

    public void EnsureOpenForPosting()
    {
        if (Status != FiscalYearStatus.Open)
            throw new InvalidOperationException("Journal posting requires an open fiscal year.");
    }

    private void EnsureOpen()
    {
        if (Status != FiscalYearStatus.Open)
            throw new InvalidOperationException("Closed fiscal years cannot be modified.");
    }

    public bool Overlaps(DateOnly start, DateOnly end) =>
        start <= EndDate && end >= StartDate;

    private static string Require(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public sealed class FiscalYearCreatedDomainEvent : IDomainEvent
{
    public FiscalYearCreatedDomainEvent(Guid fiscalYearId, DateTime occurredOnUtc) => (FiscalYearId, OccurredOnUtc) = (fiscalYearId, occurredOnUtc);
    public Guid FiscalYearId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class FiscalYearUpdatedDomainEvent : IDomainEvent
{
    public FiscalYearUpdatedDomainEvent(Guid fiscalYearId, DateTime occurredOnUtc) => (FiscalYearId, OccurredOnUtc) = (fiscalYearId, occurredOnUtc);
    public Guid FiscalYearId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class FiscalYearClosedDomainEvent : IDomainEvent
{
    public FiscalYearClosedDomainEvent(Guid fiscalYearId, DateTime occurredOnUtc) => (FiscalYearId, OccurredOnUtc) = (fiscalYearId, occurredOnUtc);
    public Guid FiscalYearId { get; }
    public DateTime OccurredOnUtc { get; }
}
