using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Persistence.Repositories;

public sealed class UserRoleRepository(ShamlDbContext dbContext)
    : IUserRoleRepository
{
    public async Task AddAsync(
        UserRole userRole,
        CancellationToken cancellationToken = default)
    {
        await dbContext.UserRoles.AddAsync(
            userRole,
            cancellationToken);
    }

    public Task<UserRole?> GetActiveRoleAsync(
        string userSocialId,
        long roleId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserSocialId == userSocialId
                     && x.RoleId == roleId
                     && x.IsActive,
                cancellationToken);
    }

    public Task<UserRole?> GetActiveAssignmentAsync(
        string userSocialId,
        long userRoleId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserSocialId == userSocialId
                     && x.Id == userRoleId
                     && x.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserRole>> GetActiveAssignmentsAsync(
        string userSocialId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.UserSocialId == userSocialId
                        && x.IsActive)
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.CenterId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleDetails>> GetActiveAssignmentDetailsAsync(
        string userSocialId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
                from assignment in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking()
                    on assignment.RoleId equals role.Id
                join center in dbContext.Centers.AsNoTracking()
                    on assignment.CenterId equals center.Id into centers
                from center in centers.DefaultIfEmpty()
                where assignment.UserSocialId == userSocialId
                      && assignment.IsActive
                orderby assignment.RoleId, assignment.CenterId
                select new
                {
                    Assignment = assignment,
                    RoleCode = role.Code,
                    RoleName = role.Name,
                    CenterName = center == null ? null : center.Name,
                    CenterId = center == null ? (long?)null : center.Id
                })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new UserRoleDetails(
                x.Assignment,
                x.RoleCode,
                x.RoleName,
                x.CenterName is null || x.CenterId is null
                    ? null
                    : $"{x.CenterName} - {x.CenterId.Value}"))
            .ToList();
    }
}
