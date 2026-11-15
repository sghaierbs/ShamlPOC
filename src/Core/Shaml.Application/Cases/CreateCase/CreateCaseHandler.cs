using Shaml.Application.Abstractions.Persistence;

namespace Shaml.Application.Cases.CreateCase;

public sealed class CreateCaseHandler(ICaseIntakeStore store)
{
    public Task<CreateCaseResponse> HandleAsync(
        CreateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SourceSystem) ||
            string.IsNullOrWhiteSpace(request.ExternalCaseId))
            throw new ArgumentException(
                "Source system and external case ID are required.");

        return store.CreateAsync(request, cancellationToken);
    }
}