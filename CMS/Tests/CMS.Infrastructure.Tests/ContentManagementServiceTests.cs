using CMS.Application.Models.Events;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Repositories.Content;
using CMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shouldly;
using System.Text.Json;

namespace CMS.Infrastructure.Tests;

public class ContentManagementServiceTests
{
    [Fact]
    public async Task ExecuteContentChangeAsync_PublishCreatesContent()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Publish, version: 1),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var content = await dbContext.Contents.Include(entity => entity.Versions).SingleAsync();
        content.PublicId.ShouldBe("content-123");
        content.Enabled.ShouldBeTrue();
        content.Versions.Single().VersionNumber.ShouldBe(1);
        content.Versions.Single().Published.ShouldBeTrue();
        JsonDocument.Parse(content.Versions.Single().Content).RootElement
            .GetProperty("title").GetString().ShouldBe("Example");
    }

    [Fact]
    public async Task ExecuteContentChangeAsync_PublishUpdatesToANewerVersionAndPreservesDisablement()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentAsync(dbContext, enabled: false);
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Publish, version: 3, payload: new { title = "Updated" }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var content = await dbContext.Contents.Include(entity => entity.Versions)
            .SingleAsync(entity => entity.PublicId == "content-123");
        content.Enabled.ShouldBeFalse();
        content.Versions.Count.ShouldBe(2);
        content.Versions.Single(version => version.VersionNumber == 2).Published.ShouldBeFalse();
        var latestVersion = content.Versions.Single(version => version.VersionNumber == 3);
        latestVersion.Published.ShouldBeTrue();
        JsonDocument.Parse(latestVersion.Content).RootElement
            .GetProperty("title").GetString().ShouldBe("Updated");
    }

    [Fact]
    public async Task ExecuteContentChangeAsync_PublishIgnoresAnOlderVersion()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Publish, version: 1, payload: new { title = "Old" }),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var content = await dbContext.Contents.Include(entity => entity.Versions).SingleAsync();
        content.Versions.Count.ShouldBe(1);
        content.Versions.Single().VersionNumber.ShouldBe(2);
        JsonDocument.Parse(content.Versions.Single().Content).RootElement
            .GetProperty("title").GetString().ShouldBe("Version 2");
    }

    [Fact]
    public async Task ExecuteContentChangeAsync_SerializesConcurrentVersionsForTheSameContent()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var firstContext = CreateDbContext(databaseName);
        await using var secondContext = CreateDbContext(databaseName);
        await SeedInitialContentAsync(firstContext);
        var barrier = new TwoCallBarrier();
        var firstService = new ConcurrentContentService(CreateService(firstContext), barrier);
        var secondService = new ConcurrentContentService(CreateService(secondContext), barrier);

        var results = await Task.WhenAll(
            firstService.ExecuteContentChangeAsync(
                CreateEvent(EventType.Publish, version: 2),
                CancellationToken.None),
            secondService.ExecuteContentChangeAsync(
                CreateEvent(EventType.Publish, version: 3),
                CancellationToken.None));

        results.ShouldAllBe(result => result.IsSuccess);
        await using var verificationContext = CreateDbContext(databaseName);
        var content = await verificationContext.Contents
            .Include(entity => entity.Versions)
            .SingleAsync();
        content.Versions.Max(version => version.VersionNumber).ShouldBe(3);
        content.Versions.Single(version => version.VersionNumber == 3).Published.ShouldBeTrue();
        content.Versions.Where(version => version.VersionNumber != 3)
            .ShouldAllBe(version => !version.Published);
    }

    [Fact]
    public async Task ExecuteContentChangeAsync_UnpublishKeepsStoredContent()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Unpublish, version: 2, payload: null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var content = await dbContext.Contents.Include(entity => entity.Versions).SingleAsync();
        content.Versions.Single().Content.ShouldBe("{\"title\":\"Version 2\"}");
        content.Versions.Single().Published.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteContentChangeAsync_DeletePhysicallyRemovesContent()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Delete, version: null, payload: null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await dbContext.Contents.CountAsync()).ShouldBe(0);
        (await dbContext.ContentVersions.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteContentChangeAsync_RejectsUnknownEventTypes()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent((EventType)999, version: 1),
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await dbContext.Contents.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ExecuteContentChangeAsync_RejectsInvalidIdentifiers(string publicId)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Publish, version: 1, publicId: publicId),
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Message.Contains("PublicId"));
        (await dbContext.Contents.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteContentChangeAsync_RejectsInvalidPublishVersions(int? version)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ExecuteContentChangeAsync(
            CreateEvent(EventType.Publish, version),
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Message.Contains("Version"));
        (await dbContext.Contents.CountAsync()).ShouldBe(0);
    }

    private static CmsDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new CmsDbContext(options);
    }

    private static ContentManagementService CreateService(CmsDbContext dbContext) =>
        new(new ContentWriteRepository(dbContext), new LoggerConfiguration().CreateLogger());

    private static async Task SeedContentAsync(CmsDbContext dbContext, bool enabled = true)
    {
        dbContext.Contents.Add(new ContentEntity
        {
            PublicId = "content-123",
            Enabled = enabled,
            Versions =
            [
                new ContentVersionEntity
                {
                    VersionNumber = 2,
                    Content = "{\"title\":\"Version 2\"}",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Published = true
                }
            ]
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedInitialContentAsync(CmsDbContext dbContext)
    {
        dbContext.Contents.Add(new ContentEntity
        {
            PublicId = "content-123",
            Enabled = true,
            Versions =
            [
                new ContentVersionEntity
                {
                    VersionNumber = 1,
                    Content = "{\"title\":\"Version 1\"}",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1),
                    Published = true
                }
            ]
        });

        await dbContext.SaveChangesAsync();
    }

    private static CmsEventModel CreateEvent(
        EventType type,
        int? version,
        object? payload = null,
        string publicId = "content-123") =>
        new()
        {
            PublicId = publicId,
            Type = type,
            Payload = payload ?? (type == EventType.Delete || type == EventType.Unpublish
                ? null
                : new { title = "Example" }),
            Version = version,
            Timestamp = DateTimeOffset.UtcNow
        };

    private sealed class TwoCallBarrier
    {
        private readonly TaskCompletionSource _bothCallsStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public async Task WaitAsync()
        {
            if (Interlocked.Increment(ref _callCount) == 2)
                _bothCallsStarted.TrySetResult();

            await _bothCallsStarted.Task;
        }
    }

    private sealed class ConcurrentContentService(
        ContentManagementService innerService,
        TwoCallBarrier barrier)
        : CMS.Application.Services.IContentManagementService
    {
        public async Task<FluentResults.Result> ExecuteContentChangeAsync(
            CmsEventModel cmsEvent,
            CancellationToken cancellationToken)
        {
            await barrier.WaitAsync();
            return await innerService.ExecuteContentChangeAsync(cmsEvent, cancellationToken);
        }
    }
}
