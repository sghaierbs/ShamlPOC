
using Microsoft.EntityFrameworkCore;
using Shaml.Application.Geography;
using Shaml.Infrastructure.Persistence;

namespace Shaml.Infrastructure.Geography;

public sealed class GeographicLookupService(ShamlDbContext dbContext) : IGeographicLookupService
{
    public async Task<IReadOnlyList<GeographicLookupDto>>
        GetRegionsAsync(
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Regions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.NameAr)
            .Select(x => new GeographicLookupDto(
                x.Id,
                x.NameAr,
                x.NameEn))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GeographicLookupDto>>
        GetCitiesAsync(
            long regionId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Cities
            .AsNoTracking()
            .Where(x => x.IsActive && x.RegionId == regionId)
            .OrderBy(x => x.NameAr)
            .Select(x => new GeographicLookupDto(
                x.Id,
                x.NameAr,
                x.NameEn))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GeographicLookupDto>>
        GetDistrictsAsync(
            long cityId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Districts
            .AsNoTracking()
            .Where(x => x.IsActive && x.CityId == cityId)
            .OrderBy(x => x.NameAr)
            .Select(x => new GeographicLookupDto(
                x.Id,
                x.NameAr,
                x.NameEn))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsValidLocationAsync(
        long regionId,
        long cityId,
        long? districtId,
        CancellationToken cancellationToken = default)
    {
        var validCity = await dbContext.Cities
            .AsNoTracking()
            .AnyAsync(
                c => c.Id == cityId
                    && c.RegionId == regionId
                    && c.IsActive
                    && dbContext.Regions.Any(
                        r => r.Id == regionId && r.IsActive),
                cancellationToken);

        if (!validCity)
            return false;

        if (districtId is null)
            return true;

        return await dbContext.Districts
            .AsNoTracking()
            .AnyAsync(
                d => d.Id == districtId.Value
                    && d.CityId == cityId
                    && d.IsActive,
                cancellationToken);
    }
}
