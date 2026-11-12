
using Shaml.Domain.Common;

namespace Shaml.Domain.Identity;

public sealed class UserRole : Entity<long>
{
    private UserRole() { } // EF Core

    public UserRole(
        string userSocialId,
        long roleId,
        RoleScope scope,
        long? centerId = null)
    {
        // Validate Social ID
        if (string.IsNullOrWhiteSpace(userSocialId) ||
            userSocialId.Length != 10 ||
            !userSocialId.All(char.IsAsciiDigit))
        {
            throw new ArgumentException(
                "SocialId must contain exactly 10 digits.",
                nameof(userSocialId));
        }

        // Validate Role ID
        if (roleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(roleId));

        // Validate Role Scope
        if (!Enum.IsDefined(scope))
            throw new ArgumentOutOfRangeException(nameof(scope));

        switch (scope)
        {
            case RoleScope.Center:
                if (centerId is null or <= 0)
                {
                    throw new ArgumentException(
                        "Center roles require a valid CenterId.",
                        nameof(centerId));
                }
                break;

            case RoleScope.Organisation:
            case RoleScope.Public:
                if (centerId.HasValue)
                {
                    throw new ArgumentException(
                        "Organisation and Public roles cannot have a CenterId.",
                        nameof(centerId));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(scope));
        }

        UserSocialId = userSocialId;
        RoleId = roleId;
        CenterId = centerId;

        IsActive = true;
        AssignedAtUtc = DateTime.UtcNow;
    }

    public string UserSocialId { get; private set; } = null!;

    public long RoleId { get; private set; }

    public long? CenterId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }
}
