using Shaml.Infrastructure.Persistence;

namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class OutboxWriter(ShamlDbContext db)
{
    public Task<Guid> AddAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        // Create the outbox message.
        var message = OutboxMessage.Create(
            eventType,
            payload);

        // Track the message in the current DbContext.
        db.OutboxMessages.Add(message);

        // Do not call SaveChangesAsync here.
        // The business operation owns the transaction.
        return Task.FromResult(message.Id);
    }
}