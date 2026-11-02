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

    private static readonly SemaphoreSlim _readSemaphore = new(1, 1);

    public async Task<Result<List<EventEntity>>> GetPendingEventsAsync(
        int batchSize,
        float errorEventsRatio,
        DateTimeOffset errorCutoffDateTime,
        CancellationToken cancellationToken
    )
    {
        await _readSemaphore.WaitAsync(cancellationToken);

        try
        {
            var errorEventsBatchSize = (int)(batchSize * errorEventsRatio);
            var pendingEventsBatchSize = batchSize - errorEventsBatchSize;

            var dbPendingEvents = _dbContext.Events
                .Where(e => e.Status == EventStatus.Pending)
                .OrderBy(e => e.CreatedDateTime)
                .Take(pendingEventsBatchSize);

            var dbErrorEvents = _dbContext.Events
                .Where(e => e.Status == EventStatus.Pending && e.LastProcessedAtDateTime < errorCutoffDateTime)
                .OrderBy(e => e.LastProcessedAtDateTime)
                .Take(errorEventsBatchSize);

            var combinedQuery = dbPendingEvents.Concat(dbErrorEvents);

            List<EventEntity> dbEvents = await combinedQuery.ToListAsync(cancellationToken);

            dbEvents.ForEach(entity => entity.Status = EventStatus.Processing);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Ok(dbEvents);
        }
        finally
        {
            _readSemaphore.Release();
        }
    }
}
