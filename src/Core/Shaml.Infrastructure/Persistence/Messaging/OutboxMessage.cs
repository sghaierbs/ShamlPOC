namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;

    public DateTime OccurredAtUtc { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }

    public OutboxStatus Status { get; private set; }
    public int Attempts { get; private set; }

    public Guid? LockToken { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        string eventType,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var now = DateTime.UtcNow;

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            OccurredAtUtc = now,
            NextAttemptAtUtc = now,
            Status = OutboxStatus.Pending
        };
    }
}

public enum OutboxStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}