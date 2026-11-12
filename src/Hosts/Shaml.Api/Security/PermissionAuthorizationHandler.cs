using Microsoft.AspNetCore.Authorization;
using Shaml.Application.Abstractions.Security;

namespace Shaml.Api.Security;

public sealed class PermissionAuthorizationHandler(IPermissionService permissionService) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var roleIdValue = context.User
            .FindFirst("active_user_role_id")?
            .Value;

        if (!long.TryParse(roleIdValue, out var userRoleId))
        {
            return;
        }

        var hasPermission = await permissionService.HasPermissionAsync(userRoleId, requirement.PermissionCode);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}