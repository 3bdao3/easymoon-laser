using ErpClink.Modules.Doctors.Application.Doctors.Models;

namespace ErpClink.Modules.Doctors.Application.Doctors;

public interface IDoctorService
{
    Task<DoctorDto> RegisterAsync(RegisterDoctorRequest request, CancellationToken cancellationToken = default);
    Task<DoctorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DoctorDto?> GetByNumberAsync(string doctorNumber, CancellationToken cancellationToken = default);
    Task<PagedDoctorsResult> SearchAsync(SearchDoctorsRequest request, CancellationToken cancellationToken = default);
    Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorClinicAssignmentDto>> GetClinicAssignmentsAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<DoctorClinicAssignmentDto> AssignToClinicAsync(Guid doctorId, AssignDoctorToClinicRequest request, CancellationToken cancellationToken = default);
    Task DeactivateClinicAssignmentAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken = default);
}

public interface IDoctorNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
