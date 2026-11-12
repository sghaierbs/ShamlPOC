using System.ComponentModel.DataAnnotations;

namespace Shaml.Application.Centers.Management;

public sealed class UpdateCenterRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long RegionId { get; init; }

    [Range(1, long.MaxValue)]
    public long CityId { get; init; }

    public long? DistrictId { get; init; }

    [Range(1, int.MaxValue)]
    public int OperationalCapacity { get; init; }

    [Range(0, int.MaxValue)]
    public int WaitingListLimit { get; init; }

    public bool IsActive { get; init; }

    public bool IsReceivingCases { get; init; }
}