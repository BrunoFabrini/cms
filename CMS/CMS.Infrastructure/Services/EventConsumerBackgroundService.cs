using CMS.Application.Models;
using CMS.Application.Services;
using CMS.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Services;

public class EventConsumerBackgroundService(
    IEventService eventService,
    IOptions<EventConsumerOptions> options,
    ILogger<EventConsumerBackgroundService> logger
) : IEventConsumerBackgroundService, IHostedService
{
    private readonly IEventService _eventService = eventService;
    private readonly EventConsumerOptions _options = options.Value;
    private readonly ILogger<EventConsumerBackgroundService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelayInSeconds), cancellationToken);

        _logger.LogInformation("Event consumer background service started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var args = new ProcessEventsArgs
            {
                BatchSize = _options.BatchSize,
                ErrorCutoffTimeInMinutes = _options.ErrorCutoffDateInMinutes,
                ErrorEventsRatio = _options.ErrorEventsRatio
            };

            var processResult = await _eventService.ProcessEventsAsync(args, cancellationToken);

            if (processResult.IsFailed)
                _logger.LogError("Error processing events: {Errors}", processResult.Errors);

            await Task.Delay(TimeSpan.FromSeconds(_options.ExecutionDelay), cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Event consumer background service stopped.");
    }
}
