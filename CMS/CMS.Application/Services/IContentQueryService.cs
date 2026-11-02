using CMS.Application.Features.Contents;
using CMS.Application.Models;
using CMS.Application.Models.Content;
using FluentResults;

namespace CMS.Application.Services;

public interface IContentQueryService
{
    Task<Result<PagedResponse<ContentModel>>> GetContentsAsync(ListContentsRequest request, CancellationToken cancellationToken);
    Task<Result<ContentModel?>> GetContentByIdAsync(string publicId, CancellationToken cancellationToken);
}
