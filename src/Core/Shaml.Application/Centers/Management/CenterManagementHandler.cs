
using Shaml.Application.Abstractions.Persistence;
using Shaml.Application.Geography;

namespace Shaml.Application.Centers.Management;

public sealed class CenterManagementHandler(
    ICenterManagementStore store,
    IGeographicLookupService geographicLookupService)
{
    public Task<IReadOnlyList<CenterDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return store.GetAllAsync(cancellationToken);
    }

    public Task<CenterDto?> GetByIdAsync(
        long centerId,
        CancellationToken cancellationToken = default)
    {
        return store.GetByIdAsync(
            centerId,
            cancellationToken);
    }

    public async Task<CenterDto?> UpdateAsync(
        long centerId,
        UpdateCenterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (centerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerId));

        if (request.IsReceivingCases && !request.IsActive)
        {
            throw new ArgumentException(
                "An inactive center cannot receive cases.");
        }

        var validLocation =
            await geographicLookupService.IsValidLocationAsync(
                request.RegionId,
                request.CityId,
                request.DistrictId,
                cancellationToken);

        if (!validLocation)
        {
            throw new ArgumentException(
                "The selected geographic location is invalid.");
        }

        return await store.UpdateAsync(
            centerId,
            request,
            cancellationToken);
    }

    public Task<CenterDto?> ReplaceDirectorAsync(
        long centerId,
        string? directorSocialId,
        CancellationToken cancellationToken = default)
    {
        if (centerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerId));

        if (directorSocialId is not null &&
            (directorSocialId.Length != 10 ||
             !directorSocialId.All(char.IsAsciiDigit)))
        {
            throw new ArgumentException(
                "Director Social ID must contain 10 digits.");
        }

        return store.ReplaceDirectorAsync(
            centerId,
            directorSocialId,
            cancellationToken);
    }
}
