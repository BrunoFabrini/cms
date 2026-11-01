using CMS.Domain.Entities;
using FluentResults;

namespace CMS.Domain.Repositories.Events;

public interface IEventWriteRepository
{
    Task<Result> CreateAsync(IEnumerable<EventEntity> events, CancellationToken cancellationToken);
}
