namespace Shaml.Application.Cases.CreateCase;

public sealed record CreateCaseResponse(
    Guid Id,
    string SourceSystem,
    string ExternalCaseId,
    long CenterId,
    string Status,
    DateTime CreatedAtUtc);