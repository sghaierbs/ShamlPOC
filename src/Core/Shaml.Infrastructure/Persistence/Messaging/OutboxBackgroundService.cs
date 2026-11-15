using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shaml.Infrastructure.Persistence.Messaging;

public sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval =
        TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Shaml outbox background service started.");

        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            // Process immediately, then every 5 seconds.
            do
            {
                try
                {
                    await using var scope =
                        scopeFactory.CreateAsyncScope();

                    var dispatcher =
                        scope.ServiceProvider
                            .GetRequiredService<OutboxDispatcher>();

                    await dispatcher.DispatchBatchAsync(
                        stoppingToken);
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
            while (await timer.WaitForNextTickAsync(
                       stoppingToken));
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }

        logger.LogInformation(
            "Shaml outbox background service stopped.");
    }
}