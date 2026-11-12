using Microsoft.AspNetCore.Authorization;

namespace Shaml.Api.Security;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException(
                "Permission code is required.",
                nameof(permissionCode));
        }

        Policy = PolicyPrefix + permissionCode;
    }
}