using CMS.Domain.Entities;
using CMS.Domain.Repositories.Events;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Repositories.Events;

public class EventWriteRepository(
    CmsDbContext dbContext
) : IEventWriteRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    private readonly DbSet<EventEntity> _events = dbContext.Set<EventEntity>();

    public async Task<Result> CreateAsync(IEnumerable<EventEntity> events, CancellationToken cancellationToken)
    {
        await _events.AddRangeAsync(events, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}