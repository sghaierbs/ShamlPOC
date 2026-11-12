using System.ComponentModel.DataAnnotations;

namespace Shaml.Application.Centers.CreateCenter;

public sealed class CreateCenterRequest
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
    
    [RegularExpression(
        @"^\d{10}$",
        ErrorMessage = "Director Social ID must contain 10 digits.")]
    public string? DirectorSocialId { get; init; }
}