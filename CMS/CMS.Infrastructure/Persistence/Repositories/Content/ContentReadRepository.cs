using CMS.Domain.Entities;
using CMS.Domain.Models.Content;
using CMS.Domain.Repositories.Content;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Repositories.Content;

internal class ContentReadRepository(
    CmsDbContext dbContext
) : IContentReadRepository
{
    private readonly CmsDbContext _dbContext = dbContext;

    public async Task<Result<int>> GetVisibleCountAsync(bool includeDisabled, CancellationToken cancellationToken)
    {
        var count = await BuildVisibleQuery(includeDisabled).CountAsync(cancellationToken);
        return Result.Ok(count);
    }

    public async Task<Result<IReadOnlyList<ContentReadModel>>> GetVisibleContentsAsync(
        bool includeDisabled,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken
    )
    {
        var contents = await ApplySorting(BuildVisibleQuery(includeDisabled), sortBy, sortDirection)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(content => content.Versions
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => new ContentReadModel
                    {
                        PublicId = content.PublicId,
                        VersionNumber = version.VersionNumber,
                        Timestamp = version.Timestamp,
                        Content = version.Content
                    })
                    .First())
                .ToListAsync(cancellationToken);

        return Result.Ok((IReadOnlyList<ContentReadModel>)contents);
    }

    public async Task<Result<ContentReadModel?>> GetVisibleContentByPublicIdAsync(
        string publicId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        var content = await BuildVisibleQuery(includeDisabled)
            .Where(content => content.PublicId == publicId)
            .Select(content => content.Versions
                .OrderByDescending(version => version.VersionNumber)
                .Select(version => new ContentReadModel
                {
                    PublicId = content.PublicId,
                    VersionNumber = version.VersionNumber,
                    Timestamp = version.Timestamp,
                    Content = version.Content
                })
                .First())
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Ok(content);
    }

    private IQueryable<ContentEntity> BuildVisibleQuery(bool includeDisabled)
    {
        var query = _dbContext.Contents
            .AsNoTracking()
            .Where(content => content.Versions.Any(version =>
                version.Published &&
                version.VersionNumber == content.Versions.Max(item => item.VersionNumber)));

        return includeDisabled
            ? query
            : query.Where(content => content.Enabled);
    }

    private static IOrderedQueryable<ContentEntity> ApplySorting(
        IQueryable<ContentEntity> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<ContentEntity> orderedQuery = sortBy.ToLowerInvariant() switch
        {
            "version" => isDescending
                ? query.OrderByDescending(content => content.Versions.Max(version => version.VersionNumber))
                : query.OrderBy(content => content.Versions.Max(version => version.VersionNumber)),
            "timestamp" => isDescending
                ? query.OrderByDescending(content => content.Versions.Max(version => version.Timestamp))
                : query.OrderBy(content => content.Versions.Max(version => version.Timestamp)),
            _ => isDescending
                ? query.OrderByDescending(content => content.PublicId)
                : query.OrderBy(content => content.PublicId)
        };

        return orderedQuery.ThenBy(content => content.Id);
    }
}
