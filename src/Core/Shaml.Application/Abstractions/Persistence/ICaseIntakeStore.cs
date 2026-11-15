using Shaml.Application.Cases.CreateCase;

namespace Shaml.Application.Abstractions.Persistence;

public interface ICaseIntakeStore
{
    Task<CreateCaseResponse> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);
}