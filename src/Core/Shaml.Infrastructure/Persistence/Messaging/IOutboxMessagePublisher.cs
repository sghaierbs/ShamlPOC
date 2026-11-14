namespace Shaml.Infrastructure.Persistence.Messaging;

public interface IOutboxMessagePublisher
{
    Task PublishAsync(Guid eventId, string eventType, string payload, CancellationToken cancellationToken);
}