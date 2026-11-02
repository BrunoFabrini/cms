using CMS.Domain.Models.Content;
using FluentResults;

namespace CMS.Domain.Repositories.Content;

public interface IContentReadRepository
{
    Task<Result<int>> GetVisibleCountAsync(bool includeDisabled, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ContentReadModel>>> GetVisibleContentsAsync(
        bool includeDisabled,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken);
    Task<Result<ContentReadModel?>> GetVisibleContentByPublicIdAsync(
        string publicId,
        bool includeDisabled,
        CancellationToken cancellationToken);
}
