
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shaml.Application.Abstractions.Security;
using Shaml.Infrastructure.Persistence;

namespace Shaml.Api.Security;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    ShamlDbContext dbContext) : ICurrentUser
{
    public string SocialId
    {
        get
        {
            var publicId = GetPublicId();

            var socialId = dbContext.Users
                .AsNoTracking()
                .Where(x => x.PublicId == publicId)
                .Select(x => x.Id)
                .SingleOrDefault();

            return socialId
                ?? throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");
        }
    }

    public long ActiveUserRoleId
    {
        get
        {
            var value = GetClaim("active_user_role_id");

            if (!long.TryParse(value, out var userRoleId))
            {
                throw new UnauthorizedAccessException(
                    "Invalid active role assignment.");
            }

            return userRoleId;
        }
    }

    private Guid GetPublicId()
    {
        var value = GetClaim(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(value, out var publicId))
        {
            throw new UnauthorizedAccessException(
                "Invalid authenticated user identifier.");
        }

        return publicId;
    }

    private string GetClaim(string claimType)
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        var value = principal.FindFirstValue(claimType);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException(
                $"Required claim '{claimType}' is missing.");
        }

        return value;
    }
}
