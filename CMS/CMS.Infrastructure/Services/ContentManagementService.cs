using CMS.Application.Models.Events;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Domain.Repositories.Content;
using CMS.Infrastructure.Utils;
using FluentResults;
using Serilog;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CMS.Infrastructure.Services;

public class ContentManagementService(
    IContentWriteRepository contentRepository,
    ILogger logger
) : IContentManagementService
{
    private readonly IContentWriteRepository _contentRepository = contentRepository;
    private readonly ILogger _logger = logger.ForContext<ContentManagementService>();
    private static readonly ConcurrentDictionary<string, ContentLockEntry> ContentLocks = new(StringComparer.Ordinal);

    public async Task<Result> ExecuteContentChangeAsync(CmsEventModel cmsEvent, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateEvent(cmsEvent);

        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        using var contentLock = await AcquireContentLockAsync(cmsEvent.PublicId, cancellationToken);

        var getContentResult = await _contentRepository.GetForUpdateByPublicIdAsync(cmsEvent.PublicId, cancellationToken);

        if (getContentResult.IsFailed)
            return Result.Fail(getContentResult.Errors);

        var contentChangeResult = cmsEvent.Type switch
        {
            EventType.Publish => await PublishContent(cmsEvent, getContentResult.Value, cancellationToken),
            EventType.Unpublish => await UnPublishContent(cmsEvent, getContentResult.Value, cancellationToken),
            EventType.Delete => await DeleteContent(cmsEvent, getContentResult.Value, cancellationToken),
            _ => Result.Fail($"Unsupported event type: {nameof(cmsEvent.Type)}")
        };

        if (contentChangeResult.IsFailed)
            return Result.Fail(contentChangeResult.Errors);

        return Result.Ok();
    }

    private static async Task<IDisposable> AcquireContentLockAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var entry = ContentLocks.GetOrAdd(publicId, _ => new ContentLockEntry());

            lock (entry)
            {
                if (!ContentLocks.TryGetValue(publicId, out var current) ||
                    !ReferenceEquals(current, entry))
                    continue;

                entry.ReferenceCount++;
            }

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken);
                return new ContentLockLease(publicId, entry);
            }
            catch
            {
                ReleaseContentLock(publicId, entry, releaseSemaphore: false);
                throw;
            }
        }
    }

    private static void ReleaseContentLock(
        string publicId,
        ContentLockEntry entry,
        bool releaseSemaphore = true)
    {
        if (releaseSemaphore)
            entry.Semaphore.Release();

        var disposeSemaphore = false;
        lock (entry)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0 &&
                ContentLocks.TryGetValue(publicId, out var current) &&
                ReferenceEquals(current, entry))
            {
                ContentLocks.TryRemove(publicId, out _);
                disposeSemaphore = true;
            }
        }

        if (disposeSemaphore)
            entry.Semaphore.Dispose();
    }

    private sealed class ContentLockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
    }

    private sealed class ContentLockLease(string publicId, ContentLockEntry entry) : IDisposable
    {
        public void Dispose() => ReleaseContentLock(publicId, entry);
    }

    private static async Task<Result> ValidateEvent(CmsEventModel cmsEvent)
    {
        var validationErrors = new List<IError>();

        if (string.IsNullOrWhiteSpace(cmsEvent.PublicId))
            validationErrors.Add(new Error("Content PublicId is null or empty"));

        if (cmsEvent.Payload is null &&
            cmsEvent.Type is not EventType.Delete and not EventType.Unpublish)
            validationErrors.Add(new Error("Content Payload is null or empty"));

        if (cmsEvent.Version.GetValueOrDefault() <= 0 && cmsEvent.Type != EventType.Delete)
            validationErrors.Add(new Error("Content Version must be greater than 0"));
        
        if (cmsEvent.Timestamp == default || cmsEvent.Timestamp is DateTimeOffset timestamp && timestamp == DateTimeOffset.MinValue)
            validationErrors.Add(new Error("Content Timestamp is invalid"));

        if (validationErrors.Count > 0)
            return Result.Fail(validationErrors);

        return Result.Ok();
    }

    private async Task<Result> PublishContent(CmsEventModel cmsEvent, ContentEntity? content, CancellationToken cancellationToken)
    {
        if ((content is null || content.Versions.Count == 0) && cmsEvent.Version != 1)
            return Result.Fail("The first published content version must be 1.");

        var versionContent = JsonSerializer.Serialize(cmsEvent.Payload, JsonConfigs.Default);

        if (content is null)
        {
            var version = new ContentVersionEntity
            {
                VersionNumber = cmsEvent.Version.GetValueOrDefault(),
                Content = versionContent,
                Timestamp = cmsEvent.Timestamp,
                Published = true
            };

            content = new ContentEntity
            {
                PublicId = cmsEvent.PublicId,
                Enabled = true,
                Versions = [ version ]
            };
        }
        else
        {
            var existingVersion = content.Versions
                .FirstOrDefault(v => v.VersionNumber == cmsEvent.Version.GetValueOrDefault());

            var lastVersion = content.Versions
                .OrderByDescending(version => version.VersionNumber)
                .FirstOrDefault();

            // If the incoming version is less than the last version we can ignore it
            if (cmsEvent.Version.GetValueOrDefault() < lastVersion?.VersionNumber)
            {
                _logger.Warning("Content PublicId {PublicId} Version {Version} ignored because it is less than the last version {LastVersion}.",
                    cmsEvent.PublicId,
                    cmsEvent.Version,
                    lastVersion?.VersionNumber
                );

                return Result.Ok();
            }
            // If the incoming version is equal to the last version, we only update it if the timestamp is newer
            else if (cmsEvent.Version.GetValueOrDefault() == lastVersion?.VersionNumber)
            {
                if (cmsEvent.Timestamp <= lastVersion.Timestamp)
                {
                    _logger.Warning("Content PublicId {PublicId} Version {Version} ignored because it is equal to the last version {LastVersion} and the timestamp is not newer.",
                        cmsEvent.PublicId,
                        cmsEvent.Version,
                        lastVersion?.VersionNumber
                    );
                    return Result.Ok();
                }

                lastVersion.Content = versionContent;
                lastVersion.Timestamp = cmsEvent.Timestamp;
                lastVersion.Published = true;
            }
            // If the incoming version is greater than the last version,
            // we add it as a new version and set the last one with published = false
            else
            {
                var version = new ContentVersionEntity
                {
                    VersionNumber = cmsEvent.Version.GetValueOrDefault(),
                    Content = versionContent,
                    Timestamp = cmsEvent.Timestamp,
                    Published = true
                };

                lastVersion?.Published = false;

                content.Versions.Add(version);
            }
        }

        var saveResult = await _contentRepository.SaveAsync(content, cancellationToken);

        if (saveResult.IsFailed)
            return Result.Fail(saveResult.Errors);

        _logger.Information("Content PublicId {PublicId} Version {Version} published successfully.",
            cmsEvent.PublicId,
            cmsEvent.Version
        );

        return Result.Ok();
    }

    private async Task<Result> UnPublishContent(CmsEventModel cmsEvent, ContentEntity? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            var errorMessage = $"Content PublicId {cmsEvent.PublicId} not found for unpublishing.";
            return Result.Fail(errorMessage);
        }

        var existingVersion = content.Versions
            .FirstOrDefault(v => v.VersionNumber == cmsEvent.Version.GetValueOrDefault());

        if (existingVersion is null)
        {
            var errorMessage = $"Content PublicId {cmsEvent.PublicId} Version {cmsEvent.Version} not found for unpublishing.";
            return Result.Fail(errorMessage);
        }

        existingVersion.Published = false;

        var saveResult = await _contentRepository.SaveAsync(content, cancellationToken);

        if (saveResult.IsFailed)
            return Result.Fail(saveResult.Errors);

        _logger.Information("Content PublicId {PublicId} Version {Version} unpublished successfully.",
            cmsEvent.PublicId,
            cmsEvent.Version
        );

        return Result.Ok();
    }

    private async Task<Result> DeleteContent(CmsEventModel cmsEvent, ContentEntity? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            _logger.Information("Content PublicId {PublicId} is already deleted", cmsEvent.PublicId);
            return Result.Ok();
        }

        var deleteResult = await _contentRepository.DeleteContentByPublicIdAsync(content, cancellationToken);

        if (deleteResult.IsFailed)
            return Result.Fail(deleteResult.Errors);

        _logger.Information("Content PublicId {PublicId} deleted successfully.", content.PublicId);

        return Result.Ok();
    }
}
