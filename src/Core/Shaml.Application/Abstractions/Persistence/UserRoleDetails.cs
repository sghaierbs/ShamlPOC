using Shaml.Domain.Identity;

namespace Shaml.Application.Abstractions.Persistence;

public sealed record UserRoleDetails(
    UserRole Assignment,
    string RoleCode,
    string RoleName,
    string? CenterDisplayName);
