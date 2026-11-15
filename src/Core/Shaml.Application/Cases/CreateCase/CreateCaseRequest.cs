using System.ComponentModel.DataAnnotations;

namespace Shaml.Application.Cases.CreateCase;

public sealed class CreateCaseRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string SourceSystem { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ExternalCaseId { get; init; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long CenterId { get; init; }
}