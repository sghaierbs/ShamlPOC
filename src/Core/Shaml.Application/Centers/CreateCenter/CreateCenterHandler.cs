using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Geography;
using Shaml.Domain.Centers;

namespace Shaml.Application.Centers.CreateCenter;

public sealed class CreateCenterHandler(
    IGeographicLookupService geographicLookupService,
    ICenterCreationStore centerCreationStore)
{
    public async Task<CreateCenterResponse> HandleAsync(
        CreateCenterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate geographic hierarchy.
        var validLocation =
            await geographicLookupService.IsValidLocationAsync(
                request.RegionId,
                request.CityId,
                request.DistrictId,
                cancellationToken);

        if (!validLocation)
        {
            throw new ArgumentException(
                "The selected region, city, or district is invalid.");
        }

        // Create Center domain entity.
        var center = new Center(
            request.Name,
            request.RegionId,
            request.CityId,
            request.DistrictId,
            request.OperationalCapacity,
            request.WaitingListLimit);

        // Save Center and optionally assign a director.
        await centerCreationStore.CreateAsync(
            center,
            request.DirectorSocialId,
            cancellationToken);

        return new CreateCenterResponse(
            center.Id,
            center.Name,
            center.DisplayName,
            center.RegionId,
            center.CityId,
            center.DistrictId,
            center.OperationalCapacity,
            center.WaitingListLimit,
            request.DirectorSocialId,
            center.IsActive,
            center.IsReceivingCases,
            center.CreatedAtUtc);
    }
}