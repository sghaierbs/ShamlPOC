namespace Shaml.Application.Abstractions.Workflows;

public interface ICaseWorkflowStarter
{
    Task<string> StartOrRecoverAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}