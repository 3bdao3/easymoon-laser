using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Scheduling.Domain.Holidays;

/// <summary>
/// Clinic-wide closed day. Minimum model for SCH-03; full holiday calendar UX is deferred.
/// </summary>
public sealed class ClinicHoliday : AggregateRoot
{
    private ClinicHoliday()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public static ClinicHoliday Create(
        Guid organizationId,
        Guid? branchId,
        DateOnly date,
        string name,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var holiday = new ClinicHoliday
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            Date = date,
            Name = name.Trim(),
            IsActive = true
        };
        holiday.SetCreated(createdBy, utcNow);
        return holiday;
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }
}
