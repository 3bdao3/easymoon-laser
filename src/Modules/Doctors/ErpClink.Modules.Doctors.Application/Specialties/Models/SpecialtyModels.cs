namespace ErpClink.Modules.Doctors.Application.Specialties.Models;

public sealed record CreateSpecialtyRequest(string Code, string Name, string? Description);
public sealed record UpdateSpecialtyRequest(string Name, string? Description);

public sealed record SpecialtyDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);
