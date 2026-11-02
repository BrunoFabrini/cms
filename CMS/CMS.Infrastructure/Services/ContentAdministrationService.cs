using CMS.Application.Services;
using CMS.Domain.Repositories.Content;
using FluentResults;

namespace CMS.Infrastructure.Services;

public class ContentAdministrationService(
    IContentWriteRepository contentRepository
) : IContentAdministrationService
{
    private readonly IContentWriteRepository _contentRepository = contentRepository;

    public async Task<Result> DisableContentAsync(string publicId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return Result.Fail("Content identifier is required.");

        return await _contentRepository.DisableContentByPublicIdAsync(publicId, cancellationToken);
    }
}
