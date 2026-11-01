using CMS.Application.Models;
using FluentResults;

namespace CMS.Application.Services;

public interface IContentManagementService
{
    Task<Result> PublishContentAsync(CmsEvent cmsEvent, CancellationToken cancellationToken);
}
