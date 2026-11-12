namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class OutboxStream
{
    public Guid Id { get; private set; }

    public string AggregateType { get; private set; } = null!;
    public string AggregateId { get; private set; } = null!;
    public string Destination { get; private set; } = null!;

    public long LastSequence { get; private set; }

    private OutboxStream() { }
}