using CMS.Application.Features.CmsEvents;
using CMS.Application.Models;
using FluentResults;

namespace CMS.Application.Services;

public interface IEventService
{
    Task<Result> IngestEventsAsync(PostEventsRequest request, CancellationToken cancellationToken);
    Task<Result> ProcessEventsAsync(ProcessEventsArgs args, CancellationToken cancellationToken);
}
