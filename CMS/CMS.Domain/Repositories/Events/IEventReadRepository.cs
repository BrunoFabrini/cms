using CMS.Domain.Entities;
using FluentResults;

namespace CMS.Domain.Repositories.Events;

public interface IEventReadRepository
{
    Task<Result<IList<EventEntity>>> GetPendingEventsAsync(
        int batchSize,
        float errorEventsRatio,
        DateTimeOffset errorCutoffDateTime,
        CancellationToken cancellationToken
    );
}
