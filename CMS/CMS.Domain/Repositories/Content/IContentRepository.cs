using CMS.Domain.Entities;
using FluentResults;

namespace CMS.Domain.Repositories.Content;

public interface IContentRepository
{
    Task<Result<ContentEntity?>> GetContentByPublicIdAsync(string publicId, CancellationToken cancellationToken);
    Task<Result> DeleteContentByPublicIdAsync(ContentEntity content, CancellationToken cancellationToken);
    Task<Result<ContentEntity>> SaveAsync(ContentEntity content, CancellationToken cancellationToken);
}
