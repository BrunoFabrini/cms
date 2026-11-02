using CMS.Application.Services;
using CMS.Domain.Repositories.Content;
using FluentResults;

namespace CMS.Infrastructure.Services;

public class ContentAdministrationService(
    IContentRepository contentRepository
) : IContentAdministrationService
{
    private readonly IContentRepository _contentRepository = contentRepository;

    public async Task<Result> DisableContentAsync(string publicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return Result.Fail("Content identifier is required.");

        return await _contentRepository.DisableContentByPublicIdAsync(publicId, cancellationToken);
    }
}
