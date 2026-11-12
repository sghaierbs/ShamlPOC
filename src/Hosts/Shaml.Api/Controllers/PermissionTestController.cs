using Microsoft.AspNetCore.Mvc;
using Shaml.Api.Security;

namespace Shaml.Api.Controllers;

[ApiController]
[Route("api/v1/permission-test")]
public sealed class PermissionTestController : ControllerBase
{
    [HttpGet("cases")]
    [HasPermission("CASE_VIEW")]
    public IActionResult ViewCases()
    {
        return Ok(new
        {
            Message = "CASE_VIEW permission granted."
        });
    }

    [HttpGet("create-session")]
    [HasPermission("SESSION_CREATE")]
    public IActionResult CreateSession()
    {
        return Ok(new
        {
            Message = "SESSION_CREATE permission granted."
        });
    }

    [HttpGet("manage-users")]
    [HasPermission("USER_MANAGE")]
    public IActionResult ManageUsers()
    {
        return Ok(new
        {
            Message = "USER_MANAGE permission granted."
        });
    }

    [HttpGet("create-center")]
    [HasPermission("CENTER_CREATE")]
    public IActionResult CreateCenter()
    {
        return Ok(new
        {
            Message = "CENTER_CREATE permission granted."
        });
    }
}