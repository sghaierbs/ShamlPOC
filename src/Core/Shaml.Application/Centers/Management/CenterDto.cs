namespace Shaml.Application.Centers.Management;

public sealed record CenterDto(
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