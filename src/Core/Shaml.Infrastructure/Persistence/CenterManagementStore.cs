
using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Centers.Management;
using Shaml.Domain.Centers;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Persistence;

public sealed class CenterManagementStore(
    ShamlDbContext dbContext) : ICenterManagementStore
{
    private const long CenterDirectorRoleId = 2;

    public async Task<IReadOnlyList<CenterDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var centers = await dbContext.Centers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var centerIds = centers.Select(x => x.Id).ToArray();

        var directors = await dbContext.UserRoles
            .AsNoTracking()
            .Where(x =>
                x.RoleId == CenterDirectorRoleId &&
                x.IsActive &&
                x.CenterId.HasValue &&
                centerIds.Contains(x.CenterId.Value))
            .Select(x => new
            {
                CenterId = x.CenterId!.Value,
                x.UserSocialId
            })
            .ToDictionaryAsync(
                x => x.CenterId,
                x => x.UserSocialId,
                cancellationToken);

        return centers
            .Select(center => ToDto(
                center,
                directors.GetValueOrDefault(center.Id)))
            .ToList();
    }

    public async Task<CenterDto?> GetByIdAsync(
        long centerId,
        CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == centerId,
                cancellationToken);

        if (center is null)
            return null;

        var directorSocialId = await dbContext.UserRoles
            .AsNoTracking()
            .Where(x =>
                x.CenterId == centerId &&
                x.RoleId == CenterDirectorRoleId &&
                x.IsActive)
            .Select(x => x.UserSocialId)
            .FirstOrDefaultAsync(cancellationToken);

        return ToDto(center, directorSocialId);
    }

    public async Task<CenterDto?> UpdateAsync(
        long centerId,
        UpdateCenterRequest request,
        CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .FirstOrDefaultAsync(
                x => x.Id == centerId,
                cancellationToken);

        if (center is null)
            return null;

        center.Rename(request.Name);

        center.UpdateLocation(
            request.RegionId,
            request.CityId,
            request.DistrictId);

        center.UpdateCapacity(
            request.OperationalCapacity,
            request.WaitingListLimit);

        if (request.IsActive)
            center.Activate();
        else
            center.Deactivate();

        if (request.IsReceivingCases)
            center.EnableReception();
        else
            center.DisableReception();

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(centerId, cancellationToken);
    }

    public async Task<CenterDto?> ReplaceDirectorAsync(
        long centerId,
        string? directorSocialId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        // Serialize director changes for the same center.
        // This requires the existing PostgreSQL Centers table.
        var center = await dbContext.Centers
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM shaml."Centers"
                 WHERE "Id" = {centerId}
                 FOR UPDATE
                 """)
            .SingleOrDefaultAsync(cancellationToken);

        if (center is null)
            return null;

        if (directorSocialId is not null)
        {
            var directorExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == directorSocialId && x.IsActive,
                    cancellationToken);

            if (!directorExists)
            {
                throw new ArgumentException(
                    "The selected director does not exist or is inactive.");
            }
        }

        var currentAssignment = await dbContext.UserRoles
            .FirstOrDefaultAsync(
                x =>
                    x.CenterId == centerId &&
                    x.RoleId == CenterDirectorRoleId &&
                    x.IsActive,
                cancellationToken);

        if (currentAssignment?.UserSocialId == directorSocialId)
        {
            await transaction.CommitAsync(cancellationToken);
            return await GetByIdAsync(centerId, cancellationToken);
        }

        if (currentAssignment is not null)
        {
            currentAssignment.Deactivate();

            // Release the partial unique index entry before
            // inserting the replacement assignment.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (directorSocialId is not null)
        {
            var assignment = new UserRole(
                directorSocialId,
                CenterDirectorRoleId,
                RoleScope.Center,
                centerId);

            await dbContext.UserRoles.AddAsync(
                assignment,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(centerId, cancellationToken);
    }

    private static CenterDto ToDto(
        Center center,
        string? directorSocialId)
    {
        return new CenterDto(
            center.Id,
            center.Name,
            center.DisplayName,
            center.RegionId,
            center.CityId,
            center.DistrictId,
            center.OperationalCapacity,
            center.WaitingListLimit,
            directorSocialId,
            center.IsActive,
            center.IsReceivingCases,
            center.CreatedAtUtc);
    }
}
