using System.ComponentModel.DataAnnotations;

namespace Shaml.Application.Users.Management;

public sealed record ManagedUserRoleResponse(
    long UserRoleId,
    long RoleId,
    string RoleCode,
    string RoleName,
    int RoleScope,
    long? CenterId,
    string? CenterDisplayName,
    bool IsDefault,
    DateTime AssignedAtUtc);

public sealed record ManagedUserResponse(
    string SocialId,
    string Username,
    string FullName,
    bool IsActive,
    DateTime CreatedAtUtc,
    IReadOnlyList<ManagedUserRoleResponse> Roles);

public sealed record AvailableRoleResponse(
    long RoleId,
    string RoleCode,
    string RoleName,
    int RoleScope);

public sealed class AssignUserRoleRequest
{
    [Range(1, long.MaxValue)]
    public long RoleId { get; init; }

    public long? CenterId { get; init; }
}
