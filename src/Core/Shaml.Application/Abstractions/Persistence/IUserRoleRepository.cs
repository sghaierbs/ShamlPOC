using Shaml.Domain.Identity;

namespace Shaml.Application.Abstractions.Persistence;

public interface IUserRoleRepository
{
    Task AddAsync(
        UserRole userRole,
        CancellationToken cancellationToken = default);

    Task<UserRole?> GetActiveRoleAsync(
        string userSocialId,
        long roleId,
        CancellationToken cancellationToken = default);

    Task<UserRole?> GetActiveAssignmentAsync(
        string userSocialId,
        long userRoleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRole>> GetActiveAssignmentsAsync(
        string userSocialId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRoleDetails>> GetActiveAssignmentDetailsAsync(
        string userSocialId,
        CancellationToken cancellationToken = default);
}
