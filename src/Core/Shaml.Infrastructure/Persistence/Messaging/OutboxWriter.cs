
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shaml.Infrastructure.Persistence;

namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class OutboxWriter(ShamlDbContext db)
{
    public async Task<Guid> AddAsync(string aggregateType, string aggregateId, string destination, string eventType, string payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "OutboxWriter requires an active database transaction.");

        var streamId = Guid.NewGuid();

        // Ensure the stream exists, without overwriting an existing stream.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO shaml."OutboxStreams"
                ("Id", "AggregateType", "AggregateId",
                 "Destination", "LastSequence")
            VALUES
                ({streamId}, {aggregateType}, {aggregateId},
                 {destination}, 0)
            ON CONFLICT ("AggregateType", "AggregateId", "Destination")
            DO NOTHING;
            """,
            cancellationToken);

        // Allocate a gap-free sequence under PostgreSQL row locking.
        var connection = db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

        command.CommandText = """
            UPDATE shaml."OutboxStreams"
            SET "LastSequence" = "LastSequence" + 1
            WHERE "AggregateType" = @aggregateType
              AND "AggregateId" = @aggregateId
              AND "Destination" = @destination
            RETURNING "Id", "LastSequence";
            """;

        void AddParameter(string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        AddParameter("aggregateType", aggregateType);
        AddParameter("aggregateId", aggregateId);
        AddParameter("destination", destination);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException(
                "Unable to allocate outbox stream sequence.");

        var actualStreamId = reader.GetGuid(0);
        var sequence = reader.GetInt64(1);

        await reader.DisposeAsync();

        var message = OutboxMessage.CreateOrdered(
            actualStreamId,
            sequence,
            eventType,
            payload);

        db.OutboxMessages.Add(message);

        // Do not call SaveChanges or Commit here.
        // The caller owns the business transaction.
        return message.Id;
    }
}
