using CMS.Application.Features.CmsEvents;
using FluentResults;

namespace CMS.Application.Services;

public interface IEventService
{
    Task<Result> ProcessAsync(PostEventsRequest request, CancellationToken cancellationToken);
}
