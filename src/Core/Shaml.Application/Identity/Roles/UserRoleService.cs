
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Abstractions.Security;
using Shaml.Application.Identity.Login;
using Shaml.Domain.Identity;

namespace Shaml.Application.Identity.Roles;

public sealed class UserRoleService(
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IAccessTokenService accessTokenService,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<UserRoleResponse>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);

        var assignments =
            await userRoleRepository.GetActiveAssignmentDetailsAsync(
                user.SocialId,
                cancellationToken);

        return assignments
            .Select(x => new UserRoleResponse(
                UserRoleId: x.Assignment.Id,
                RoleId: x.Assignment.RoleId,
                RoleCode: x.RoleCode,
                RoleName: x.RoleName,
                CenterId: x.Assignment.CenterId,
                CenterDisplayName: x.CenterDisplayName,
                IsDefault: user.DefaultUserRoleId == x.Assignment.Id,
                IsActive: x.Assignment.IsActive))
            .ToList();
    }

    public async Task SetDefaultRoleAsync(
        long userRoleId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);

        var assignment =
            await userRoleRepository.GetActiveAssignmentAsync(
                user.SocialId,
                userRoleId,
                cancellationToken);

        if (assignment is null)
        {
            throw new UnauthorizedAccessException(
                "The selected role assignment is not available.");
        }

        user.SetDefaultRole(assignment.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginResponse> SwitchRoleAsync(
        long userRoleId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);

        var assignment =
            await userRoleRepository.GetActiveAssignmentAsync(
                user.SocialId,
                userRoleId,
                cancellationToken);

        if (assignment is null)
        {
            throw new UnauthorizedAccessException(
                "The selected role assignment is not available.");
        }

        // Invalidate the previous JWT atomically.
        var tokenVersion =
            await unitOfWork.IncrementTokenVersionAsync(
                user.SocialId,
                cancellationToken);

        var token = accessTokenService.GenerateToken(
            user.PublicId,
            assignment.Id,
            tokenVersion);

        return new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 900,
            ActiveUserRoleId: assignment.Id);
    }

    private async Task<User> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetBySocialIdAsync(
            currentUser.SocialId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user is not active.");
        }

        return user;
    }
}
