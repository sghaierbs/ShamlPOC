using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shaml.Application.Cases.CreateCase;

namespace Shaml.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cases")]
public sealed class CasesController(
    CreateCaseHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCaseRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(request, ct);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                code = "INVALID_REQUEST",
                message = ex.Message
            });
        }
        catch (CaseIntakeException ex)
        {
            var httpStatus = ex.Code switch
            {
                "CENTER_NOT_FOUND" => 404,
                _ => 409
            };

            return StatusCode(httpStatus, new
            {
                code = ex.Code,
                message = ex.Message
            });
        }
    }
}