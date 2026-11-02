using CMS.Application.Features.CmsEvents;
using CMS.Application.Models.Events;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Domain.Repositories.Events;
using CMS.Infrastructure.Options;
using CMS.Infrastructure.Utils;
using FluentResults;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;
using System.Text.Json;

namespace CMS.Infrastructure.Services;

internal class EventService(
    IEventWriteRepository eventWriteRepository,
    IEventReadRepository eventReadRepository,
    IContentManagementService contentManagementService,
    IOptions<EventIngestionOptions> options,
    ILogger logger
) : IEventService
{
    private readonly IEventWriteRepository _eventWriteRepository = eventWriteRepository;
    private readonly IEventReadRepository _eventReadRepository = eventReadRepository;
    private readonly IContentManagementService _contentManagementService = contentManagementService;
    private readonly EventIngestionOptions _options = options.Value;
    private readonly ILogger _logger = logger.ForContext<EventService>();

    public async Task<Result> IngestEventsAsync(PostEventsRequest request, CancellationToken cancellationToken)
    {
        if (request.Count == 0)
        {
            _logger.Information("No events to ingest.");
            return Result.Ok();
        }

        if (request.Count > _options.MaxBatchSize)
        {
            var errorMessage = $"Received {request.Count} events, which exceeds the limit of {_options.MaxBatchSize}.";
            _logger.Warning(errorMessage);
            return Result.Fail(errorMessage);
        }

        _logger.Information("Ingesting {EventCount} events.", request.Count);

        var serializedEvents = await Task.WhenAll(
            request.Select(async cmsEvent =>
            {
                using var stream = new MemoryStream();

                await JsonSerializer.SerializeAsync(stream, cmsEvent, JsonConfigs.Default);

                return Encoding.UTF8.GetString(stream.ToArray());
            })
        );

        var events = serializedEvents.Select(serializedEvent => new EventEntity
        {
            Payload = serializedEvent, 
            Status = EventStatus.Pending,
            ProcessingAttempts = 0,
            ErrorMessage = string.Empty,
            LastProcessedAtDateTime = null
        }).ToList();

        var createEventsResult = await _eventWriteRepository.CreateAsync(events, cancellationToken);

        if (createEventsResult.IsFailed)
            return Result.Fail(createEventsResult.Errors);

        _logger.Information("Successfully ingested {EventCount} events.", events.Count);

        return Result.Ok();
    }

    public async Task<Result> ProcessEventsAsync(ProcessEventsArgs args, CancellationToken cancellationToken)
    {
        var errorCutOffDateTime = DateTimeOffset.UtcNow.AddMinutes(-args.ErrorCutoffTimeInMinutes);

        var getPendingEventsResult = await _eventReadRepository.GetPendingEventsAsync(
            args.BatchSize,
            args.ErrorEventsRatio,
            errorCutOffDateTime,
            cancellationToken
        );

        if (getPendingEventsResult.IsFailed)
            return Result.Fail(getPendingEventsResult.Errors);

        foreach (var eventEntity in getPendingEventsResult.Value)
        {
            eventEntity.Status = EventStatus.Processing;

            var processEventResult = await ProcessEventAsync(eventEntity, cancellationToken);

            if (processEventResult.IsFailed)
            {
                eventEntity.ProcessingAttempts++;
                eventEntity.ErrorMessage = string.Join("; ", processEventResult.Errors.Select(e => e.Message));

                eventEntity.Status = eventEntity.ProcessingAttempts >= args.MaxProcessingAttempts
                    ? EventStatus.Quarantined
                    : EventStatus.Error;
            }
            else
            {
                eventEntity.Status = EventStatus.Processed;
                eventEntity.ErrorMessage = string.Empty;
            }

            eventEntity.LastProcessedAtDateTime = DateTimeOffset.UtcNow;

            var updateEventResult = await _eventWriteRepository.UpdateAsync(eventEntity, cancellationToken);

            if (updateEventResult.IsFailed)
                _logger.Error("Failed to update event with ID: {EventId}. Errors: {Errors}",
                    eventEntity.Id,
                    string.Join("; ", updateEventResult.Errors.Select(e => e.Message)));
        }

        return Result.Ok();
    }

    private async Task<Result> ProcessEventAsync(EventEntity eventEntity, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Information("Processing event with ID: {EventId}", eventEntity.Id);

            var cmsEvent = JsonSerializer.Deserialize<CmsEventModel>(
                eventEntity.Payload,
                JsonConfigs.Default);

            if (cmsEvent == null)
                return Result.Fail("Event payload is null.");

            var contentChangeResult = await _contentManagementService.ExecuteContentChangeAsync(cmsEvent, cancellationToken);

            if (contentChangeResult.IsFailed)
            {
                _logger.Error("Failed to update content from event with ID: {EventId}. Errors: {Errors}",
                    eventEntity.Id,
                    string.Join("; ", contentChangeResult.Errors.Select(e => e.Message)));
                return Result.Fail(contentChangeResult.Errors);
            }

            _logger.Information("Successfully processed event with ID: {EventId}", eventEntity.Id);

            return Result.Ok();
        }
        catch (JsonException ex)
        {
            var errorMessage = $"Serialization failed for event payload with ID: {eventEntity.Id}.";
            _logger.Error(ex, errorMessage);
            return Result.Fail($"{errorMessage} Exception{ex.Message}");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to process event with ID: {eventEntity.Id}.";
            _logger.Error(ex, errorMessage);
            return Result.Fail($"{errorMessage} Exception{ex.Message}");
        }
    }
}
