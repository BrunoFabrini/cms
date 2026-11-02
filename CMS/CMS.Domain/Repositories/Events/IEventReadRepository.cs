using CMS.Domain.Entities;
using FluentResults;

namespace CMS.Domain.Repositories.Events;

public interface IEventReadRepository
{
    Task<Result<List<EventEntity>>> GetPendingEventsAsync(
        int batchSize,
        float errorEventsRatio,
        DateTimeOffset errorCutoffDateTime,
        CancellationToken cancellationToken
    );

    Task<Result<string?>> GetLatestProcessedDeletePayloadAsync(
        string publicId,
        CancellationToken cancellationToken
    );
}
