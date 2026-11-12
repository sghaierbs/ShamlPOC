using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shaml.Application.Geography;

namespace Shaml.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/lookups")]
public sealed class LookupsController(IGeographicLookupService lookupService) : ControllerBase
{
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions(
        CancellationToken cancellationToken)
    {
        var result = await lookupService.GetRegionsAsync(
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(
        [FromQuery] long regionId,
        CancellationToken cancellationToken)
    {
        if (regionId <= 0)
            return BadRequest("Valid regionId is required.");

        var result = await lookupService.GetCitiesAsync(
            regionId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts(
        [FromQuery] long cityId,
        CancellationToken cancellationToken)
    {
        if (cityId <= 0)
            return BadRequest("Valid cityId is required.");

        var result = await lookupService.GetDistrictsAsync(
            cityId,
            cancellationToken);

        return Ok(result);
    }
}