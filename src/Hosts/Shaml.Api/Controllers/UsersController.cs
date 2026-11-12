using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shaml.Api.Security;
using Shaml.Application.Users.Management;
using Shaml.Application.Abstractions.Security;

namespace Shaml.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(
    UserManagementHandler handler,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [HasPermission("USER_VIEW")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await handler.GetUsersAsync(cancellationToken));

    [HttpGet("available-roles")]
    [HasPermission("USER_MANAGE")]
    public async Task<IActionResult> GetAvailableRoles(CancellationToken cancellationToken) =>
        Ok(await handler.GetAvailableRolesAsync(cancellationToken));

    [HttpGet("{socialId}")]
    [HasPermission("USER_VIEW")]
    public async Task<IActionResult> GetById(
        string socialId,
        CancellationToken cancellationToken)
    {
        var result = await handler.GetUserAsync(socialId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{socialId}/roles")]
    [HasPermission("USER_MANAGE")]
    public async Task<IActionResult> AssignRole(
        string socialId,
        [FromBody] AssignUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.AssignRoleAsync(
                socialId,
                request,
                cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{socialId}/roles/{userRoleId:long}")]
    [HasPermission("USER_MANAGE")]
    public async Task<IActionResult> WithdrawRole(
        string socialId,
        long userRoleId,
        CancellationToken cancellationToken)
    {
        if (currentUser.SocialId == socialId &&
            currentUser.ActiveUserRoleId == userRoleId)
        {
            return BadRequest(new
            {
                message = "You cannot withdraw the role used by your current session. Switch roles first."
            });
        }

        try
        {
            var result = await handler.WithdrawRoleAsync(
                socialId,
                userRoleId,
                cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
