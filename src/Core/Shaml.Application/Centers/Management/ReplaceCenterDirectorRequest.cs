using System.ComponentModel.DataAnnotations;

namespace Shaml.Application.Centers.Management;

public sealed class ReplaceCenterDirectorRequest
{
    // Null means remove the currently assigned director.
    [RegularExpression(
        @"^\d{10}$",
        ErrorMessage = "Director Social ID must contain 10 digits.")]
    public string? DirectorSocialId { get; init; }
}