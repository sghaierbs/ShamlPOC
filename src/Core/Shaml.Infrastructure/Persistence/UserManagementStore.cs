using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Users.Management;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Persistence;

public sealed class UserManagementStore(ShamlDbContext dbContext)
    : IUserManagementStore
{
    public async Task<IReadOnlyList<ManagedUserResponse>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var socialIds = await dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var users = new List<ManagedUserResponse>(socialIds.Count);
        foreach (var socialId in socialIds)
        {
            var user = await GetUserAsync(socialId, cancellationToken);
            if (user is not null)
                users.Add(user);
        }

        return users;
    }

    public async Task<ManagedUserResponse?> GetUserAsync(
        string socialId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == socialId, cancellationToken);

        if (user is null)
            return null;

        var rows = await (
                from assignment in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking()
                    on assignment.RoleId equals role.Id
                join center in dbContext.Centers.AsNoTracking()
                    on assignment.CenterId equals center.Id into centers
                from center in centers.DefaultIfEmpty()
                where assignment.UserSocialId == socialId && assignment.IsActive
                orderby role.Name, assignment.CenterId
                select new
                {
                    Assignment = assignment,
                    Role = role,
                    CenterName = center == null ? null : center.Name,
                    CenterId = center == null ? (long?)null : center.Id
                })
            .ToListAsync(cancellationToken);

        var roles = rows
            .Select(x => new ManagedUserRoleResponse(
                x.Assignment.Id,
                x.Role.Id,
                x.Role.Code,
                x.Role.Name,
                (int)x.Role.Scope,
                x.Assignment.CenterId,
                x.CenterName is null || x.CenterId is null
                    ? null
                    : $"{x.CenterName} - {x.CenterId.Value}",
                user.DefaultUserRoleId == x.Assignment.Id,
                x.Assignment.AssignedAtUtc))
            .ToList();

        return new ManagedUserResponse(
            user.SocialId,
            user.Username,
            user.FullName,
            user.IsActive,
            user.CreatedAtUtc,
            roles);
    }

    public async Task<IReadOnlyList<AvailableRoleResponse>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Scope
            })
            .ToListAsync(cancellationToken);

        // Cast only after EF converts the varchar value to RoleScope.
        // Casting inside the query generates CAST("Scope" AS integer).
        return roles
            .Select(x => new AvailableRoleResponse(
                x.Id,
                x.Code,
                x.Name,
                (int)x.Scope))
            .ToList();
    }

    public async Task<ManagedUserResponse?> AssignRoleAsync(
        string socialId,
        long roleId,
        long? centerId,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users
            .AnyAsync(x => x.Id == socialId && x.IsActive, cancellationToken);
        if (!userExists)
            return null;

        var role = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId && x.IsActive, cancellationToken)
            ?? throw new ArgumentException("The selected role is not available.");

        if (role.Scope == RoleScope.Center)
        {
            if (centerId is null or <= 0)
                throw new ArgumentException("A center is required for this role.");

            var centerExists = await dbContext.Centers
                .AsNoTracking()
                .AnyAsync(x => x.Id == centerId && x.IsActive, cancellationToken);
            if (!centerExists)
                throw new ArgumentException("The selected center is not available.");
        }
        else if (centerId.HasValue)
        {
            throw new ArgumentException("The selected role is not center-scoped.");
        }

        if (role.Code == RoleCodes.CenterDirector && centerId.HasValue)
        {
            var centerHasDirector = await dbContext.UserRoles
                .AsNoTracking()
                .AnyAsync(
                    x => x.RoleId == role.Id
                         && x.CenterId == centerId
                         && x.IsActive,
                    cancellationToken);
            if (centerHasDirector)
                throw new ArgumentException("The selected center already has an active director.");
        }

        var duplicate = await dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(
                x => x.UserSocialId == socialId
                     && x.RoleId == roleId
                     && x.CenterId == centerId
                     && x.IsActive,
                cancellationToken);
        if (duplicate)
            throw new ArgumentException("The user already has this role assignment.");

        await dbContext.UserRoles.AddAsync(
            new UserRole(socialId, role.Id, role.Scope, centerId),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetUserAsync(socialId, cancellationToken);
    }

    public async Task<ManagedUserResponse?> WithdrawRoleAsync(
        string socialId,
        long userRoleId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == socialId, cancellationToken);
        if (user is null)
            return null;

        var assignment = await dbContext.UserRoles
            .FirstOrDefaultAsync(
                x => x.Id == userRoleId
                     && x.UserSocialId == socialId
                     && x.IsActive,
                cancellationToken);
        if (assignment is null)
            return null;

        var activeRoleCount = await dbContext.UserRoles
            .CountAsync(
                x => x.UserSocialId == socialId && x.IsActive,
                cancellationToken);
        if (activeRoleCount <= 1)
            throw new ArgumentException("A user's final active role cannot be withdrawn.");

        assignment.Deactivate();
        if (user.DefaultUserRoleId == assignment.Id)
            user.ClearDefaultRole();

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(socialId, cancellationToken);
    }
}
