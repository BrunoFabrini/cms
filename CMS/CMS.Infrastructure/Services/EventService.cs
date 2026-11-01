using CMS.Application.Features.CmsEvents;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Domain.Repositories.Events;
using FluentResults;
using System.Text;
using System.Text.Json;

namespace CMS.Infrastructure.Services;

internal class EventService(
    IEventWriteRepository eventWriteRepository
) : IEventService
{
    private readonly IEventWriteRepository _eventWriteRepository = eventWriteRepository;

    public async Task<Result> ProcessAsync(PostEventsRequest request, CancellationToken cancellationToken)
    {
        var serializedEvents = await Task.WhenAll(
            request.Select(async cmsEvent =>
            {
                using var stream = new MemoryStream();

                //TODO 1 - Create global serialziation options and reuse here
                await JsonSerializer.SerializeAsync(stream, cmsEvent);

                return Encoding.UTF8.GetString(stream.ToArray());
            })
        );

        var events = serializedEvents.Select(serializedEvent => new EventEntity
        {
            Payload = serializedEvent, 
            Status = EventStatus.Pending,
            ErrorMessage = string.Empty,
            LastProcessedAtDateTime = null
        }).ToList();

        var createEventsResult = await _eventWriteRepository.CreateAsync(events, cancellationToken);

        if (createEventsResult.IsFailed)
            return Result.Fail(createEventsResult.Errors);

        return Result.Ok();
    }

    private Result ValidateEvents(PostEventsRequest request)
    {
        //TODO - Validation will occur on event processing
        return Result.Ok();
    }
}
