using CMS.Application.Models;
using CMS.Application.Services;
using CMS.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace CMS.Infrastructure.Services;

public class EventConsumerBackgroundService(
    IEventService eventService,
    IOptions<EventConsumerOptions> options,
    ILogger logger
) : IHostedService
{
    private readonly IEventService _eventService = eventService;
    private readonly EventConsumerOptions _options = options.Value;
    private readonly ILogger _logger = logger.ForContext<EventConsumerBackgroundService>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelayInSeconds), cancellationToken);

        _logger.Information("Event consumer background service started.");

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
                _logger.Error("Error processing events: {Errors}", processResult.Errors);

            await Task.Delay(TimeSpan.FromSeconds(_options.ExecutionDelay), cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Event consumer background service stopped.");
    }
}
