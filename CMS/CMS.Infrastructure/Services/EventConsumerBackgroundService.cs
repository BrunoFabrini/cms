using CMS.Application.Models.Events;
using CMS.Application.Services;
using CMS.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace CMS.Infrastructure.Services;

public class EventConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<EventConsumerOptions> options,
    ILogger logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly EventConsumerOptions _options = options.Value;
    private readonly ILogger _logger = logger.ForContext<EventConsumerBackgroundService>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelayInSeconds), stoppingToken);

        _logger.Information("Event consumer background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var args = new ProcessEventsArgs
            {
                BatchSize = _options.BatchSize,
                ErrorCutoffTimeInMinutes = _options.ErrorCutoffDateInMinutes,
                ErrorEventsRatio = _options.ErrorEventsRatio,
                MaxProcessingAttempts = _options.MaxProcessingAttempts
            };

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                var processResult = await eventService.ProcessEventsAsync(args, stoppingToken);

                if (processResult.IsFailed)
                    _logger.Error("Error processing events: {Errors}", processResult.Errors);
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.ExecutionDelay), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Event consumer background service stopped.");
        await base.StopAsync(cancellationToken);
    }
}
