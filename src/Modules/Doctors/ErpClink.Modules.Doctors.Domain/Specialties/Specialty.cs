using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Doctors.Domain.Specialties;

public sealed class Specialty : AggregateRoot
{
    private Specialty()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Specialty Create(
        Guid organizationId,
        string code,
        string name,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var specialty = new Specialty
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = Normalize(description),
            IsActive = true
        };
        specialty.SetCreated(createdBy, utcNow);
        return specialty;
    }

    public void Update(string name, string? description, string? updatedBy, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = Normalize(description);
        SetUpdated(updatedBy, utcNow);
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
