using Shaml.Infrastructure.Persistence.Messaging;

namespace Shaml.Worker;

public sealed class OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Shaml Outbox Worker started.");

        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var dispatcher = scope.ServiceProvider
                        .GetRequiredService<OutboxDispatcher>();

                    await dispatcher.DispatchBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Unexpected error during outbox dispatch.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }

        logger.LogInformation("Shaml Outbox Worker stopped.");
    }
}