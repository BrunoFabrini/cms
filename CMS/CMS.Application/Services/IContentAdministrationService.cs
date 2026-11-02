using FluentResults;

namespace CMS.Application.Services;

public interface IContentAdministrationService
{
    Task<Result> DisableContentAsync(string publicId, CancellationToken cancellationToken);
}
