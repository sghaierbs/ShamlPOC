using Shaml.Domain.Centers;

namespace Shaml.Application.Abstractions.Persistence;

public interface ICenterCreationStore
{
    Task CreateAsync(
        Center center,
        string? directorSocialId,
        CancellationToken cancellationToken = default);
}