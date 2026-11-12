using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shaml.Application.Abstractions.Security;

namespace Shaml.Api.Security;

public sealed class JwtAccessTokenService(IConfiguration configuration) : IAccessTokenService
{
    public string GenerateToken(
        Guid userPublicId,
        long activeUserRoleId,
        long tokenVersion)
    {
        var jwt = configuration.GetSection("Jwt");

        var key = jwt["Key"]
                  ?? throw new InvalidOperationException(
                      "JWT signing key is missing.");

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userPublicId.ToString()),

            new Claim(
                "active_user_role_id",
                activeUserRoleId.ToString()),

            new Claim(
                "token_version",
                tokenVersion.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}