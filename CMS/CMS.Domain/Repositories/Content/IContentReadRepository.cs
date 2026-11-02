using CMS.Domain.Entities;
using FluentResults;

namespace CMS.Domain.Repositories.Content;

public interface IContentReadRepository
{
    Task<Result<int>> GetVisibleCountAsync(bool includeDisabled, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ContentEntity>>> GetVisibleContentsAsync(
        bool includeDisabled,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken);
    Task<Result<ContentEntity?>> GetVisibleContentByPublicIdAsync(
        string publicId,
        bool includeDisabled,
        CancellationToken cancellationToken);
}
