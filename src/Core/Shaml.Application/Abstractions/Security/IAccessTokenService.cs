namespace Shaml.Application.Abstractions.Security;

public interface IAccessTokenService
{
    string GenerateToken(
        Guid userPublicId,
        long activeUserRoleId,
        long tokenVersion);
}