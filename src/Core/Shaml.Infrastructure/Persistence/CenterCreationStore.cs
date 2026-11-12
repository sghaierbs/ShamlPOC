
using System.Data;
using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Persistence;
using Shaml.Domain.Centers;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Persistence;

public sealed class CenterCreationStore(
    ShamlDbContext dbContext) : ICenterCreationStore
{
    private const long CenterDirectorRoleId = 2;

    public async Task CreateAsync(
        Center center,
        string? directorSocialId,
        CancellationToken cancellationToken = default)
    {
        // Validate the director before inserting anything.
        if (directorSocialId is not null)
        {
            var directorExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.Id == directorSocialId &&
                        user.IsActive,
                    cancellationToken);

            if (!directorExists)
            {
                throw new ArgumentException(
                    "The selected Center Director does not exist or is inactive.",
                    nameof(directorSocialId));
            }
        }

        // Center creation and director assignment are atomic.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // Insert the center.
        await dbContext.Centers.AddAsync(
            center,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        // PostgreSQL has now generated Center.Id.
        if (directorSocialId is not null)
        {
            var assignment = new UserRole(
                directorSocialId,
                CenterDirectorRoleId,
                RoleScope.Center,
                center.Id);

            await dbContext.UserRoles.AddAsync(
                assignment,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
