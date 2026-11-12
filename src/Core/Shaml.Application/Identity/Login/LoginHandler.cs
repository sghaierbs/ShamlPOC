
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Abstractions.Security;
using Shaml.Domain.Identity;

namespace Shaml.Application.Identity.Login;

public sealed class LoginHandler(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    IUnitOfWork unitOfWork)
{
    private const long PublicUserRoleId = 5;

    public async Task<LoginResponse> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByUsernameAsync(
            request.Username,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !passwordHasher.VerifyPassword(
                request.Password,
                user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Invalid username or password.");
        }

        UserRole? activeRole = null;

        if (user.DefaultUserRoleId.HasValue)
        {
            activeRole = await userRoleRepository
                .GetActiveAssignmentAsync(
                    user.SocialId,
                    user.DefaultUserRoleId.Value,
                    cancellationToken);
        }

        activeRole ??= await userRoleRepository.GetActiveRoleAsync(
            user.SocialId,
            PublicUserRoleId,
            cancellationToken);

        if (activeRole is null)
        {
            throw new UnauthorizedAccessException(
                "No active role assignment is available.");
        }

        // A new login invalidates previously issued JWTs.
        var tokenVersion =
            await unitOfWork.IncrementTokenVersionAsync(
                user.SocialId,
                cancellationToken);

        var token = accessTokenService.GenerateToken(
            user.PublicId,
            activeRole.Id,
            tokenVersion);

        return new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 900,
            ActiveUserRoleId: activeRole.Id);
    }
}
