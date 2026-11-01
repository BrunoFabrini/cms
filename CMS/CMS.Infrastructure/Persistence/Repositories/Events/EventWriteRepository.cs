using CMS.Domain.Entities;
using CMS.Domain.Repositories.Events;
using FluentResults;

namespace CMS.Infrastructure.Persistence.Repositories.Events;

public class EventWriteRepository(
    CmsDbContext dbContext
) : IEventWriteRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    public async Task<Result> CreateAsync(IEnumerable<EventEntity> events, CancellationToken cancellationToken)
    {
        await _dbContext.Events.AddRangeAsync(events, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> UpdateAsync(EventEntity revent, CancellationToken cancellationToken)
    {
        _dbContext.Events.Update(revent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}