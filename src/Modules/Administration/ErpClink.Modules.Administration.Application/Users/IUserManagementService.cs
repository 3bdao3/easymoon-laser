using ErpClink.Modules.Administration.Application.Users.Models;

namespace ErpClink.Modules.Administration.Application.Users;

public interface IUserManagementService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task<UserDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);
}
