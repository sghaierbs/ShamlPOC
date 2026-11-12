using Microsoft.AspNetCore.Authorization;

namespace Shaml.Api.Security;

public sealed class PermissionRequirement(
    string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}