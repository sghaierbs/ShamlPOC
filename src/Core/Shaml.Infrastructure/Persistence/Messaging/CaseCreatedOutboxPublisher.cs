using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shaml.Application.Abstractions.Workflows;

namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class CaseCreatedOutboxPublisher(ShamlDbContext db, ICaseWorkflowStarter workflowStarter, ILogger<CaseCreatedOutboxPublisher> logger) : IOutboxMessagePublisher
{
    public async Task PublishAsync(
        Guid eventId,
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        if (eventType != "CaseCreated")
        {
            throw new NotSupportedException(
                $"Unsupported outbox event: {eventType}");
        }

        using var document = JsonDocument.Parse(payload);

        var caseId = document.RootElement
            .GetProperty("CaseId")
            .GetGuid();

        // Cases created before workflow registration
        // was implemented are intentionally excluded.
        var hasRegistration = await db.CaseWorkflowInstances
            .AsNoTracking()
            .AnyAsync(
                x => x.CaseId == caseId,
                cancellationToken);

        if (!hasRegistration)
        {
            logger.LogInformation(
                "Skipping legacy CaseCreated event {EventId} " +
                "for case {CaseId}: no workflow registration.",
                eventId,
                caseId);

            return;
        }

        var instanceId = await workflowStarter
            .StartOrRecoverAsync(
                caseId,
                cancellationToken);

        logger.LogInformation(
            "Case workflow ready. CaseId={CaseId}, " +
            "WorkflowInstanceId={WorkflowInstanceId}",
            caseId,
            instanceId);
    }
}