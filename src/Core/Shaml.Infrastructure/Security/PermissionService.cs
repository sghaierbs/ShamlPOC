using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Security;
using Shaml.Infrastructure.Persistence;

namespace Shaml.Infrastructure.Security;

public sealed class PermissionService(ShamlDbContext dbContext) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(long activeUserRoleId, string permissionCode, CancellationToken cancellationToken = default)
    {
        if (activeUserRoleId <= 0 ||
            string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        return await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join rolePermission in dbContext.RolePermissions.AsNoTracking()
                on userRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.Id
            where userRole.Id == activeUserRoleId
                  && userRole.IsActive
                  && permission.Code == permissionCode
            select permission.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        long activeUserRoleId,
        CancellationToken cancellationToken = default)
    {
        if (activeUserRoleId <= 0)
            return [];

        return await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join rolePermission in dbContext.RolePermissions.AsNoTracking()
                    on userRole.RoleId equals rolePermission.RoleId
                join permission in dbContext.Permissions.AsNoTracking()
                    on rolePermission.PermissionId equals permission.Id
                where userRole.Id == activeUserRoleId
                      && userRole.IsActive
                orderby permission.Code
                select permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
