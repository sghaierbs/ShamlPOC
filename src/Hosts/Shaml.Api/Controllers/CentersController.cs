
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shaml.Api.Security;
using Shaml.Application.Centers.CreateCenter;
using Shaml.Application.Centers.Management;

namespace Shaml.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/centers")]
public sealed class CentersController(CreateCenterHandler createCenterHandler, CenterManagementHandler managementHandler) : ControllerBase
{
    [HttpPost]
    [HasPermission("CENTER_CREATE")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCenterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await createCenterHandler.HandleAsync(
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    [HasPermission("CENTER_VIEW")]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await managementHandler.GetAllAsync(
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission("CENTER_VIEW")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await managementHandler.GetByIdAsync(
            id,
            cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission("CENTER_UPDATE")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateCenterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await managementHandler.UpdateAsync(
                id,
                request,
                cancellationToken);

            return result is null
                ? NotFound()
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:long}/director")]
    [HasPermission("CENTER_UPDATE")]
    public async Task<IActionResult> ReplaceDirector(
        long id,
        [FromBody] ReplaceCenterDirectorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await managementHandler.ReplaceDirectorAsync(
                id,
                request.DirectorSocialId,
                cancellationToken);

            return result is null
                ? NotFound()
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
