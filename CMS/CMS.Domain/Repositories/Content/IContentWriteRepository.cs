using CMS.Domain.Entities;
using FluentResults;

namespace CMS.Domain.Repositories.Content;

public interface IContentWriteRepository
{
    Task<Result<ContentEntity?>> GetForUpdateByPublicIdAsync(string publicId, CancellationToken cancellationToken);
    Task<Result> DisableContentByPublicIdAsync(string publicId, CancellationToken cancellationToken);
    Task<Result> DeleteContentByPublicIdAsync(ContentEntity content, CancellationToken cancellationToken);
    Task<Result<ContentEntity>> SaveAsync(ContentEntity content, CancellationToken cancellationToken);
}
