using Microsoft.AspNetCore.Identity;
using Shaml.Application.Abstractions.Security;
using Shaml.Domain.Identity;

namespace Shaml.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool VerifyPassword(
        string password,
        string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(
            null!,
            passwordHash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}