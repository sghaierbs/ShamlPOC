using Shaml.Application.Users.Management;

namespace Shaml.Application.Abstractions.Persistence;

public interface IUserManagementStore
{
    Task<IReadOnlyList<ManagedUserResponse>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<ManagedUserResponse?> GetUserAsync(
        string socialId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableRoleResponse>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);

    Task<ManagedUserResponse?> AssignRoleAsync(
        string socialId,
        long roleId,
        long? centerId,
        CancellationToken cancellationToken = default);

    Task<ManagedUserResponse?> WithdrawRoleAsync(
        string socialId,
        long userRoleId,
        CancellationToken cancellationToken = default);
}
