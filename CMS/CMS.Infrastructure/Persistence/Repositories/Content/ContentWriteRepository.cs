using CMS.Domain.Entities;
using CMS.Domain.Repositories.Content;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Repositories.Content;

internal class ContentWriteRepository(
    CmsDbContext dbContext
) : IContentWriteRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    public async Task<Result<ContentEntity?>> GetForUpdateByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        var content = await _dbContext.Contents
            .Include(content => content.Versions)
            .FirstOrDefaultAsync(content => content.PublicId == publicId, cancellationToken);

        return Result.Ok(content);
    }

    public async Task<Result> DisableContentByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        var dbContent = await _dbContext.Contents
            .FirstOrDefaultAsync(content => content.PublicId == publicId, cancellationToken);

        if (dbContent is null)
            return Result.Fail("Content not found.");

        dbContent.Enabled = false;

        await SaveAsync(dbContent, cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<ContentEntity>> SaveAsync(ContentEntity content, CancellationToken cancellationToken)
    {
        if (content.Id.GetValueOrDefault() == 0)
            _dbContext.Contents.Add(content);
        else if (_dbContext.Entry(content).State == EntityState.Detached)
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
