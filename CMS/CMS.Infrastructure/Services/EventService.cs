using CMS.Application.Features.CmsEvents;
using CMS.Application.Services;
using FluentResults;

namespace CMS.Infrastructure.Services;

internal class EventService : IEventService
{
    public Task<Result> ProcessAsync(PostEventsRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
