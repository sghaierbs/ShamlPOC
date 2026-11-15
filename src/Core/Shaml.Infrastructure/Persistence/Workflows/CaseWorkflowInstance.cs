namespace Shaml.Infrastructure.Persistence.Workflows;

public sealed class CaseWorkflowInstance
{
    public Guid CaseId { get; set; }

    public string WorkflowDefinitionId { get; set; }
        = "ShamlCaseLifecycle";

    public string? WorkflowInstanceId { get; set; }

    public string Status { get; set; } = "PENDING";

    public Guid? StartToken { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; }
        = DateTime.UtcNow;
}