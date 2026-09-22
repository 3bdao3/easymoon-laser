using ErpClink.Modules.Doctors.Application.Specialties.Models;

namespace ErpClink.Modules.Doctors.Application.Specialties;

public interface ISpecialtyService
{
    Task<SpecialtyDto> CreateAsync(CreateSpecialtyRequest request, CancellationToken cancellationToken = default);
    Task<SpecialtyDto> UpdateAsync(Guid id, UpdateSpecialtyRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpecialtyDto>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
