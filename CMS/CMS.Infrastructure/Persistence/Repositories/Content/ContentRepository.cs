using CMS.Domain.Entities;
using CMS.Domain.Repositories.Content;
using FluentResults;

namespace CMS.Infrastructure.Persistence.Repositories.Content;

internal class ContentRepository(
    CmsDbContext dbContext
) : IContentRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    public async Task<Result<ContentEntity?>> GetContentByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        var content = _dbContext.Contents.FirstOrDefault(content => content.PublicId == publicId);

        return Result.Ok(content);
    }
}
