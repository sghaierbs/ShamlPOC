namespace Shaml.Application.Abstractions.Persistence;

public interface ICaseAssignmentStore
{
    Task<CaseAssignmentResult> AssignAutomaticallyAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}

public sealed record CaseAssignmentResult(
    Guid CaseId,
    long? UserRoleId,
    string Outcome);