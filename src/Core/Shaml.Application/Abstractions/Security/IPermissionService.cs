namespace Shaml.Application.Abstractions.Security;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(long activeUserRoleId, string permissionCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPermissionsAsync(
        long activeUserRoleId,
        CancellationToken cancellationToken = default);
}
