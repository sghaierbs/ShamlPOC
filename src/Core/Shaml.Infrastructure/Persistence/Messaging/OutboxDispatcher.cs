
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shaml.Infrastructure.Persistence;

namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class OutboxDispatcher(ShamlDbContext db, IOutboxMessagePublisher publisher, ILogger<OutboxDispatcher> logger)
{
    private const int BatchSize = 1;
    private const int MaxAttempts = 10;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken = default)
    {
        var messages = await ClaimAsync(cancellationToken);

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await publisher.PublishAsync(
                    message.Id,
                    message.EventType,
                    message.Payload,
                    cancellationToken);

                await CompleteAsync(message, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Outbox delivery failed for {EventId}",
                    message.Id);

                await FailAsync(message, ex, cancellationToken);
            }
        }

        return messages.Count;
    }

    private async Task<List<ClaimedMessage>> ClaimAsync(CancellationToken ct)
    {
        var messages = new List<ClaimedMessage>();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();

        await using var command = new NpgsqlCommand(
            """
            WITH candidates AS (
                SELECT "Id"
                FROM shaml."OutboxMessages"
                WHERE
                    ("Status" = 0 AND "NextAttemptAtUtc" <= now())
                    OR
                    ("Status" = 1 AND "LockedUntilUtc" <= now())
                ORDER BY "OccurredAtUtc", "Id"
                FOR UPDATE SKIP LOCKED
                LIMIT @batchSize
            )
            UPDATE shaml."OutboxMessages" AS o
            SET
                "Status" = 1,
                "Attempts" = o."Attempts" + 1,
                "LockToken" = gen_random_uuid(),
                "LockedUntilUtc" = now() + interval '2 minutes'
            FROM candidates AS c
            WHERE o."Id" = c."Id"
            RETURNING
                o."Id",
                o."EventType",
                o."Payload"::text,
                o."LockToken",
                o."Attempts";
            """,
            connection,
            (NpgsqlTransaction)transaction.GetDbTransaction());

        command.Parameters.AddWithValue("batchSize", BatchSize);

        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                messages.Add(new ClaimedMessage(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetGuid(3),
                    reader.GetInt32(4)));
            }
        }

        await transaction.CommitAsync(ct);

        return messages;
    }

    private async Task CompleteAsync(ClaimedMessage message, CancellationToken ct)
    {
        var updated = await db.OutboxMessages
            .Where(x =>
                x.Id == message.Id &&
                x.Status == OutboxStatus.Processing &&
                x.LockToken == message.LockToken)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status,
                    OutboxStatus.Completed)
                .SetProperty(x => x.ProcessedAtUtc,
                    DateTime.UtcNow)
                .SetProperty(x => x.LockToken,
                    (Guid?)null)
                .SetProperty(x => x.LockedUntilUtc,
                    (DateTime?)null)
                .SetProperty(x => x.LastError,
                    (string?)null), ct);

        if (updated == 0)
            logger.LogWarning(
                "Outbox completion skipped: lease ownership lost for {EventId}",
                message.Id);
    }

    private async Task FailAsync(
        ClaimedMessage message,
        Exception exception,
        CancellationToken ct)
    {
        var terminal = message.Attempts >= MaxAttempts;

        var delaySeconds = Math.Min(
            300,
            Math.Pow(2, Math.Min(message.Attempts, 8)));

        var nextAttempt =
            DateTime.UtcNow.AddSeconds(delaySeconds);

        var error = exception.ToString();
        if (error.Length > 4000)
            error = error[..4000];

        var updated = await db.OutboxMessages
            .Where(x =>
                x.Id == message.Id &&
                x.Status == OutboxStatus.Processing &&
                x.LockToken == message.LockToken)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status,
                    terminal
                        ? OutboxStatus.Failed
                        : OutboxStatus.Pending)
                .SetProperty(x => x.NextAttemptAtUtc,
                    nextAttempt)
                .SetProperty(x => x.LockToken,
                    (Guid?)null)
                .SetProperty(x => x.LockedUntilUtc,
                    (DateTime?)null)
                .SetProperty(x => x.LastError,
                    error), ct);

        if (updated == 0)
            logger.LogWarning(
                "Outbox failure update skipped: lease ownership lost for {EventId}",
                message.Id);
    }

    private sealed record ClaimedMessage(
        Guid Id,
        string EventType,
        string Payload,
        Guid LockToken,
        int Attempts);
}
