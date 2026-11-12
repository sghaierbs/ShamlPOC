using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Shaml.Api.Security;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permissionCode = policyName[HasPermissionAttribute.PolicyPrefix.Length..];

        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permissionCode)).Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}