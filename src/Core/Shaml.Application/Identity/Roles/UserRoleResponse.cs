namespace Shaml.Application.Identity.Roles;

public sealed record UserRoleResponse(
    long UserRoleId,
    long RoleId,
    string RoleCode,
    string RoleName,
    long? CenterId,
    string? CenterDisplayName,
    bool IsDefault,
    bool IsActive);
