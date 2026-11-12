namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class InboxMessage
{
    public Guid EventId { get; private set; }
    public string ConsumerName { get; private set; } = null!;
    public DateTime ProcessedAtUtc { get; private set; }

    private InboxMessage() { }

    public InboxMessage(Guid eventId, string consumerName)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException(
                "Event ID cannot be empty.", nameof(eventId));

        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        EventId = eventId;
        ConsumerName = consumerName;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}