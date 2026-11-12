using Shaml.Application.Centers.Management;

namespace Shaml.Application.Abstractions.Persistence;

public interface ICenterManagementStore
{
    Task<IReadOnlyList<CenterDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<CenterDto?> GetByIdAsync(
        long centerId,
        CancellationToken cancellationToken = default);

    Task<CenterDto?> UpdateAsync(
        long centerId,
        UpdateCenterRequest request,
        CancellationToken cancellationToken = default);

    Task<CenterDto?> ReplaceDirectorAsync(
        long centerId,
        string? directorSocialId,
        CancellationToken cancellationToken = default);
}