using Shaml.Application.Abstractions.Persistence;

namespace Shaml.Application.Users.Management;

public sealed class UserManagementHandler(IUserManagementStore store)
{
    public Task<IReadOnlyList<ManagedUserResponse>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        store.GetUsersAsync(cancellationToken);

    public Task<ManagedUserResponse?> GetUserAsync(
        string socialId,
        CancellationToken cancellationToken = default) =>
        store.GetUserAsync(socialId, cancellationToken);

    public Task<IReadOnlyList<AvailableRoleResponse>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default) =>
        store.GetAvailableRolesAsync(cancellationToken);

    public Task<ManagedUserResponse?> AssignRoleAsync(
        string socialId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken = default) =>
        store.AssignRoleAsync(
            socialId,
            request.RoleId,
            request.CenterId,
            cancellationToken);

    public Task<ManagedUserResponse?> WithdrawRoleAsync(
        string socialId,
        long userRoleId,
        CancellationToken cancellationToken = default) =>
        store.WithdrawRoleAsync(socialId, userRoleId, cancellationToken);
}
