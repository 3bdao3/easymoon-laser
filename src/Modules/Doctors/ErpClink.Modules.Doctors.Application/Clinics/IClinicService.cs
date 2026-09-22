using ErpClink.Modules.Doctors.Application.Clinics.Models;

namespace ErpClink.Modules.Doctors.Application.Clinics;

public interface IClinicService
{
    Task<ClinicDto> CreateAsync(CreateClinicRequest request, CancellationToken cancellationToken = default);
    Task<ClinicDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedClinicsResult> SearchAsync(SearchClinicsRequest request, CancellationToken cancellationToken = default);
    Task<ClinicDto> UpdateAsync(Guid id, UpdateClinicRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
