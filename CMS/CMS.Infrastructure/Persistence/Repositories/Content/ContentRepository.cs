using CMS.Domain.Entities;
using CMS.Domain.Repositories.Content;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Repositories.Content;

internal class ContentRepository(
    CmsDbContext dbContext
) : IContentRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    public async Task<Result<ContentEntity?>> GetContentByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        var content = _dbContext.Contents
            .Include(content => content.Versions)
            .FirstOrDefault(content => content.PublicId == publicId);

        return Result.Ok(content);
    }

    public async Task<Result<ContentEntity>> SaveAsync(ContentEntity content, CancellationToken cancellationToken)
    {
        if (content.Id.GetValueOrDefault() == 0)
            _dbContext.Contents.Add(content);
        else
            _dbContext.Contents.Update(content);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(content);
    }

    public async Task<Result> DeleteContentByPublicIdAsync(ContentEntity content, CancellationToken cancellationToken)
    {
        _dbContext.Contents.Remove(content);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
