using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Doctors.Domain.Nurses;

/// <summary>
/// Nurse clinical staff profile (separate from Doctor; authentication remains in Identity).
/// </summary>
public sealed class Nurse : AggregateRoot
{
    private Nurse()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string NurseNumber { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Nurse Register(
        Guid organizationId,
        Guid branchId,
        string nurseNumber,
        string? userId,
        string firstName,
        string lastName,
        string? displayName,
        string? phoneNumber,
        string? email,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nurseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var nurse = new Nurse
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            NurseNumber = nurseNumber.Trim(),
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? $"{firstName.Trim()} {lastName.Trim()}"
                : displayName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            IsActive = true
        };
        nurse.SetCreated(createdBy, utcNow);
        return nurse;
    }

    public void Update(
        string firstName,
        string lastName,
        string? displayName,
        string? phoneNumber,
        string? email,
        string? userId,
        string? updatedBy,
        DateTime utcNow)
    {
        if (!IsActive) throw new InvalidOperationException("Nurse is inactive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"{FirstName} {LastName}" : displayName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
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
}
