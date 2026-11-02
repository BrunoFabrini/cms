using CMS.Application.Models.Events;
using FluentResults;

namespace CMS.Application.Services;

public interface IContentManagementService
{
    Task<Result> ExecuteContentChangeAsync(CmsEventModel cmsEvent, CancellationToken cancellationToken);
}
