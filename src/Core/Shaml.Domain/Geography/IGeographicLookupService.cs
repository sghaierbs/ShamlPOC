namespace Shaml.Application.Geography;

public interface IGeographicLookupService
{
    Task<IReadOnlyList<GeographicLookupDto>> GetRegionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeographicLookupDto>> GetCitiesAsync(
        long regionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeographicLookupDto>> GetDistrictsAsync(
        long cityId,
        CancellationToken cancellationToken = default);

    Task<bool> IsValidLocationAsync(
        long regionId,
        long cityId,
        long? districtId,
        CancellationToken cancellationToken = default);
}