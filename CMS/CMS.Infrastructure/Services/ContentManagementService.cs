using CMS.Application.Models;
using CMS.Application.Services;
using CMS.Domain.Repositories.Content;
using FluentResults;
using Serilog;

namespace CMS.Infrastructure.Services;

public class ContentManagementService(
    IContentRepository contentRepository,
    ILogger logger
) : IContentManagementService
{
    private readonly IContentRepository _contentRepository = contentRepository;
    private readonly ILogger _logger = logger.ForContext<ContentManagementService>();

    public async Task<Result> PublishContentAsync(CmsEvent cmsEvent, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateEvent(cmsEvent);

        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        var getContentResult = await _contentRepository.GetContentByPublicIdAsync(cmsEvent.PublicId, cancellationToken);

        if (getContentResult.IsFailed)
            return Result.Fail(getContentResult.Errors);

        //TODO - Implement content update logic here

        _logger.Information("Content PublicId {PublicId} Version {Version} published successfully.",
            cmsEvent.PublicId,
            cmsEvent.Version
        );

        return Result.Ok();
    }

    private static async Task<Result> ValidateEvent(CmsEvent cmsEvent)
    {
        var validationErrors = new List<IError>();

        if (string.IsNullOrWhiteSpace(cmsEvent.PublicId))
            validationErrors.Add(new Error("Content PublicId is null or empty"));

        if (string.IsNullOrWhiteSpace(cmsEvent.Type))
            validationErrors.Add(new Error("Content Type is null or empty"));

        if (string.IsNullOrWhiteSpace(cmsEvent.Payload))
            validationErrors.Add(new Error("Content Payload is null or empty"));

        if (cmsEvent.Version <= 0)
            validationErrors.Add(new Error("Content Version must be greater than 0"));

        if (validationErrors.Count > 0)
            return Result.Fail(validationErrors);

        return Result.Ok();
    }
}
