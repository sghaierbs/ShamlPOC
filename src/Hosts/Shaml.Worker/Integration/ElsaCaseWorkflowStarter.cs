
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Parameters;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shaml.Application.Abstractions.Workflows;
using Shaml.Infrastructure.Persistence;

namespace Shaml.Worker.Integration;

public sealed class ElsaCaseWorkflowStarter(
    ShamlDbContext db,
    IWorkflowRuntime workflowRuntime,
    ILogger<ElsaCaseWorkflowStarter> logger)
    : ICaseWorkflowStarter
{
    public async Task<string> StartOrRecoverAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (caseId == Guid.Empty)
        {
            throw new ArgumentException(
                "CaseId cannot be empty.",
                nameof(caseId));
        }

        // A dedicated PostgreSQL connection holds the advisory
        // lock throughout Elsa startup and Shaml reconciliation.
        var connectionString =
            db.Database.GetConnectionString()
            ?? throw new InvalidOperationException(
                "Shaml database connection string is missing.");

        await using var lockConnection =
            new NpgsqlConnection(connectionString);

        await lockConnection.OpenAsync(cancellationToken);

        var lockKey = $"shaml:workflow-start:{caseId:D}";

        await using (var command = new NpgsqlCommand(
            "SELECT pg_advisory_lock(hashtextextended(@key, 0))",
            lockConnection))
        {
            command.Parameters.AddWithValue("key", lockKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            return await StartUnderLockAsync(
                caseId,
                cancellationToken);
        }
        finally
        {
            // Release explicitly. Closing the dedicated connection
            // also releases session-level advisory locks.
            await using var unlock = new NpgsqlCommand(
                "SELECT pg_advisory_unlock(hashtextextended(@key, 0))",
                lockConnection);

            unlock.Parameters.AddWithValue("key", lockKey);

            await unlock.ExecuteNonQueryAsync(
                CancellationToken.None);
        }
    }

    private async Task<string> StartUnderLockAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var registration = await db.CaseWorkflowInstances
            .SingleOrDefaultAsync(
                x => x.CaseId == caseId,
                cancellationToken);

        if (registration is null)
        {
            throw new InvalidOperationException(
                $"No workflow registration exists for case {caseId}.");
        }

        var instanceId = $"shaml-case-{caseId:D}";
        var correlationId = caseId.ToString("D");

        if (registration.Status == "STARTED")
        {
            if (registration.WorkflowInstanceId != instanceId)
            {
                throw new InvalidOperationException(
                    $"Workflow registration mismatch for case {caseId}.");
            }

            return instanceId;
        }

        if (registration.Status != "PENDING" &&
            registration.Status != "STARTING" &&
            registration.Status != "FAILED")
        {
            throw new InvalidOperationException(
                $"Unsupported workflow startup status: " +
                registration.Status);
        }

        // Check Elsa's persisted instance before attempting
        // to start anything.
        var client = await workflowRuntime.CreateClientAsync(
            instanceId,
            cancellationToken);

        var exists = await client.InstanceExistsAsync(
            cancellationToken);

        if (exists)
        {
            logger.LogInformation(
                "Recovering existing Elsa workflow. " +
                "CaseId={CaseId}, InstanceId={InstanceId}",
                caseId,
                instanceId);
        }
        else
        {
            logger.LogInformation(
                "Starting Elsa workflow. " +
                "CaseId={CaseId}, DefinitionId={DefinitionId}",
                caseId,
                registration.WorkflowDefinitionId);

#pragma warning disable CS0618
            await workflowRuntime.StartWorkflowAsync(
                registration.WorkflowDefinitionId,
                new StartWorkflowRuntimeParams
                {
                    InstanceId = instanceId,
                    CorrelationId = correlationId,
                    Input = new Dictionary<string, object>
                    {
                        ["CaseId"] = correlationId
                    },
                    CancellationToken = cancellationToken
                });
#pragma warning restore CS0618

            // Confirm that Elsa persisted the instance.
            if (!await client.InstanceExistsAsync(
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Elsa instance {instanceId} was not found " +
                    "after workflow startup.");
            }
        }

        // Reconcile Shaml only after Elsa existence is confirmed.
        var now = DateTime.UtcNow;

        registration.WorkflowInstanceId = instanceId;
        registration.Status = "STARTED";
        registration.StartToken = null;
        registration.StartedAtUtc ??= now;
        registration.UpdatedAtUtc = now;
        registration.LastError = null;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Shaml workflow registration reconciled. " +
            "CaseId={CaseId}, InstanceId={InstanceId}",
            caseId,
            instanceId);

        return instanceId;
    }
}
