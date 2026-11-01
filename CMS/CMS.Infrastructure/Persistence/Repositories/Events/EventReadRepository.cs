using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Domain.Repositories.Events;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Repositories.Events;

internal class EventReadRepository(
    CmsDbContext dbContext
) : IEventReadRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    public async Task<Result<IList<EventEntity>>> GetPendingEventsAsync(
        int batchSize,
        float errorEventsRatio,
        DateTimeOffset errorCutoffDateTime,
        CancellationToken cancellationToken
    )
    {
        //TODO - Add SemaphoreSlim to read data from table to avoid concurrency issues
        //when multiple instances of the service are running
        var errorEventsBatchSize = (int)(batchSize * errorEventsRatio);
        var pendingEventsBatchSize = batchSize - errorEventsBatchSize;

        var dbPendingEvents = _dbContext.Events
            .Where(e => e.Status == EventStatus.Pending)
            .Take(pendingEventsBatchSize);

        var dbErrorEvents = _dbContext.Events
            .Where(e => e.Status == EventStatus.Pending && e.LastProcessedAtDateTime < errorCutoffDateTime)
            .Take(errorEventsBatchSize);

        var combinedQuery = dbPendingEvents.Concat(dbErrorEvents);

        IList<EventEntity> dbEvents = await combinedQuery.ToListAsync(cancellationToken);

        return Result.Ok(dbEvents);
    }
}
