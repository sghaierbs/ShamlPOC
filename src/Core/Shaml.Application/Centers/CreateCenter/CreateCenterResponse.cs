namespace Shaml.Application.Centers.CreateCenter;

public sealed record CreateCenterResponse(
    long Id,
    string Name,
    string DisplayName,
    long RegionId,
    long CityId,
    long? DistrictId,
    int OperationalCapacity,
    int WaitingListLimit,
    string? DirectorSocialId,
    bool IsActive,
    bool IsReceivingCases,
    DateTime CreatedAtUtc);