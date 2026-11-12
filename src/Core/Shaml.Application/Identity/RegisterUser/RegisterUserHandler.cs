
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Abstractions.Security;
using Shaml.Domain.Identity;

namespace Shaml.Application.Identity.RegisterUser;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
{
    private const long PublicUserRoleId = 5;

    public async Task<RegisterUserResponse> HandleAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 12)
        {
            throw new ArgumentException(
                "Password must contain at least 12 characters.");
        }

        if (await userRepository.ExistsBySocialIdAsync(
                request.SocialId, cancellationToken))
        {
            throw new InvalidOperationException(
                "A user with this SocialId already exists.");
        }

        if (await userRepository.ExistsByUsernameAsync(
                request.Username, cancellationToken))
        {
            throw new InvalidOperationException(
                "Username already exists.");
        }

        var user = new User(
            request.SocialId,
            request.Username,
            request.FullName);

        var passwordHash = passwordHasher.HashPassword(
            request.Password);

        user.SetPasswordHash(passwordHash);

        var publicUserRole = new UserRole(
            userSocialId: user.SocialId,
            roleId: PublicUserRoleId,
            scope: RoleScope.Public);

        await userRepository.AddAsync(user, cancellationToken);

        await userRoleRepository.AddAsync(
            publicUserRole, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse(
            user.SocialId,
            user.Username,
            user.FullName);
    }
}
