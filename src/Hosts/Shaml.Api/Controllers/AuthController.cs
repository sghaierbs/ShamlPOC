
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shaml.Application.Identity.Login;
using Shaml.Application.Identity.RegisterUser;
using Shaml.Application.Identity.Roles;
using Shaml.Application.Abstractions.Security;

namespace Shaml.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    RegisterUserHandler registerUserHandler,
    LoginHandler loginHandler,
    UserRoleService userRoleService,
    IPermissionService permissionService,
    ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUserHandler.HandleAsync(
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<UserRoleResponse>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var result = await userRoleService.GetRolesAsync(
            cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("permissions")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetPermissions(
        CancellationToken cancellationToken)
    {
        var result = await permissionService.GetPermissionsAsync(
            currentUser.ActiveUserRoleId,
            cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPut("default-role")]
    public async Task<IActionResult> SetDefaultRole(
        [FromBody] SelectRoleRequest request,
        CancellationToken cancellationToken)
    {
        await userRoleService.SetDefaultRoleAsync(
            request.UserRoleId,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("switch-role")]
    public async Task<ActionResult<LoginResponse>> SwitchRole(
        [FromBody] SelectRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userRoleService.SwitchRoleAsync(
            request.UserRoleId,
            cancellationToken);

        return Ok(result);
    }
}
