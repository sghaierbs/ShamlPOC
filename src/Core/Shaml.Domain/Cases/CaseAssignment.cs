using Shaml.Domain.Common;

namespace Shaml.Domain.Cases;

public sealed class CaseAssignment : Entity<Guid>
{
    private CaseAssignment() { } // EF Core

    public CaseAssignment(
        Guid caseId,
        long userRoleId,
        long? assignedByUserRoleId = null)
    {
        if (caseId == Guid.Empty)
            throw new ArgumentException("Invalid CaseId.");

        if (userRoleId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(userRoleId));

        Id = Guid.NewGuid();
        CaseId = caseId;
        UserRoleId = userRoleId;
        AssignedByUserRoleId = assignedByUserRoleId;
        AssignedAtUtc = DateTime.UtcNow;
    }

    public Guid CaseId { get; private set; }

    public long UserRoleId { get; private set; }

    public long? AssignedByUserRoleId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    public DateTime? EndedAtUtc { get; private set; }

    public string? EndReason { get; private set; }

    public bool IsActive => EndedAtUtc == null;

    public void End(string reason)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "Assignment has already ended.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "End reason is required.");

        EndedAtUtc = DateTime.UtcNow;
        EndReason = reason.Trim();
    }
}