using Shaml.Domain.Common;

namespace Shaml.Domain.Cases;

public sealed class Case : Entity<Guid>
{
    public string ExternalCaseId { get; private set; } = null!;
    public string SourceSystem { get; private set; } = null!;
    public long CenterId { get; private set; }
    public CaseStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private Case() { }

    public Case(
        string externalCaseId,
        string sourceSystem,
        long centerId,
        CaseStatus initialStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalCaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSystem);

        if (externalCaseId.Trim().Length > 100 ||
            sourceSystem.Trim().Length > 100)
            throw new ArgumentException("Identifier exceeds 100 characters.");

        if (centerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(centerId));

        if (initialStatus is not (
            CaseStatus.UnderStudy or CaseStatus.PendingAssignment))
            throw new ArgumentException("Invalid initial case status.");

        Id = Guid.NewGuid();
        ExternalCaseId = externalCaseId.Trim();
        SourceSystem = sourceSystem.Trim();
        CenterId = centerId;
        Status = initialStatus;
        CreatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }
}