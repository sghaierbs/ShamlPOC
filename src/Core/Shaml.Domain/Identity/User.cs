
using Shaml.Domain.Common;

namespace Shaml.Domain.Identity;

public sealed class User : Entity<string>
{
    public string SocialId => Id;

    public Guid PublicId { get; private set; } = Guid.NewGuid();

    public string Username { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public long? DefaultUserRoleId { get; private set; }

    public long TokenVersion { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private User() { } // EF Core

    public User(
        string socialId,
        string username,
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(socialId) ||
            socialId.Length != 10 ||
            !socialId.All(char.IsAsciiDigit))
        {
            throw new ArgumentException(
                "SocialId must contain exactly 10 digits.",
                nameof(socialId));
        }

        Id = socialId;

        Username = string.IsNullOrWhiteSpace(username)
            ? throw new ArgumentException("Username is required.")
            : username.Trim();

        FullName = string.IsNullOrWhiteSpace(fullName)
            ? throw new ArgumentException("Full name is required.")
            : fullName.Trim();

        IsActive = true;
        TokenVersion = 0;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.");

        PasswordHash = passwordHash;
    }

    public void SetDefaultRole(long userRoleId)
    {
        if (userRoleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userRoleId));

        DefaultUserRoleId = userRoleId;
    }

    public void ClearDefaultRole()
    {
        DefaultUserRoleId = null;
    }

    public void IncrementTokenVersion()
    {
        checked
        {
            TokenVersion++;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
