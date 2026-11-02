using CMS.Application.Features.CmsEvents;
using CMS.Application.Models.Events;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Domain.Repositories.Events;
using CMS.Infrastructure.Options;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Repositories.Content;
using CMS.Infrastructure.Persistence.Repositories.Events;
using CMS.Infrastructure.Services;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using System.Text.Json;

namespace CMS.Infrastructure.Tests;

public class EventServiceTests
{
    [Fact]
    public async Task IngestEventsAsync_StoresEventsAsPending()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateEventService(dbContext, maxBatchSize: 2);

        var result = await service.IngestEventsAsync(
            [CreateEvent()],
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var storedEvent = await dbContext.Events.SingleAsync();
        storedEvent.Status.ShouldBe(EventStatus.Pending);
        storedEvent.ProcessingAttempts.ShouldBe(0);
        storedEvent.ErrorMessage.ShouldBe(string.Empty);
        storedEvent.LastProcessedAtDateTime.ShouldBeNull();
        using var payload = JsonDocument.Parse(storedEvent.Payload);
        payload.RootElement.GetProperty("Id").GetString()
            .ShouldBe("content-123");
    }

    [Fact]
    public async Task IngestEventsAsync_AcceptsAnEmptyBatch()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateEventService(dbContext);

        var result = await service.IngestEventsAsync([], CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await dbContext.Events.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task IngestEventsAsync_RejectsBatchesAboveTheConfiguredLimit()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateEventService(dbContext, maxBatchSize: 1);

        var result = await service.IngestEventsAsync(
            [CreateEvent(), CreateEvent()],
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldContain("exceeds the limit of 1");
        (await dbContext.Events.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task ProcessEventsAsync_MarksSuccessfullyProcessedEvents()
    {
        await using var dbContext = CreateDbContext();
        var contentService = new RecordingContentManagementService();
        var service = CreateEventService(dbContext, contentService: contentService);
        dbContext.Events.Add(CreatePendingEntity());
        await dbContext.SaveChangesAsync();

        var result = await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var processedEvent = await dbContext.Events.SingleAsync();
        processedEvent.Status.ShouldBe(EventStatus.Processed);
        processedEvent.ErrorMessage.ShouldBe(string.Empty);
        processedEvent.LastProcessedAtDateTime.ShouldNotBeNull();
        contentService.Events.Count.ShouldBe(1);
        contentService.Events[0].PublicId.ShouldBe("content-123");
    }

    [Fact]
    public async Task ProcessEventsAsync_ProcessesMultipleEventsInOneBatch()
    {
        await using var dbContext = CreateDbContext();
        var contentService = new RecordingContentManagementService();
        var service = CreateEventService(dbContext, contentService: contentService);
        dbContext.Events.AddRange(
            CreatePendingEntity(publicId: "content-123"),
            CreatePendingEntity(publicId: "content-456"));
        await dbContext.SaveChangesAsync();

        var result = await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await dbContext.Events.CountAsync(entity => entity.Status == EventStatus.Processed)).ShouldBe(2);
        contentService.Events.Select(cmsEvent => cmsEvent.PublicId)
            .ShouldBe(["content-123", "content-456"]);
    }

    [Fact]
    public async Task ProcessEventsAsync_ContinuesAfterAnInvalidEventInTheBatch()
    {
        await using var dbContext = CreateDbContext();
        var contentService = new RecordingContentManagementService();
        var service = CreateEventService(dbContext, contentService: contentService);
        dbContext.Events.AddRange(
            new EventEntity
            {
                Payload = "not-json",
                Status = EventStatus.Pending,
                ProcessingAttempts = 0,
                ErrorMessage = string.Empty,
                CreatedDateTime = DateTimeOffset.UtcNow.AddMinutes(-1)
            },
            CreatePendingEntity());
        await dbContext.SaveChangesAsync();

        var result = await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var events = await dbContext.Events.OrderBy(entity => entity.Id).ToListAsync();
        events[0].Status.ShouldBe(EventStatus.Error);
        events[1].Status.ShouldBe(EventStatus.Processed);
        contentService.Events.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessEventsAsync_ProcessesVersionsForTheSameContentInOrder()
    {
        await using var dbContext = CreateDbContext();
        var writeRepository = new ContentWriteRepository(dbContext);
        var contentService = new ContentManagementService(
            writeRepository,
            new LoggerConfiguration().CreateLogger());
        var service = CreateEventService(dbContext, contentService: contentService);
        dbContext.Events.AddRange(
            CreatePendingEntity(CreateEvent(version: 1), createdDateTime: DateTimeOffset.UtcNow.AddMinutes(-2)),
            CreatePendingEntity(CreateEvent(version: 2), createdDateTime: DateTimeOffset.UtcNow.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();

        var result = await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var content = await dbContext.Contents.Include(entity => entity.Versions).SingleAsync();
        content.Versions.Count.ShouldBe(2);
        content.Versions.Single(version => version.VersionNumber == 1).Published.ShouldBeFalse();
        content.Versions.Single(version => version.VersionNumber == 2).Published.ShouldBeTrue();
        (await dbContext.Events.CountAsync(entity => entity.Status == EventStatus.Processed)).ShouldBe(2);
    }

    [Fact]
    public async Task ProcessEventsAsync_DoesNotRecreateContentFromAnOldPublishAfterDeletion()
    {
        await using var dbContext = CreateDbContext();
        var contentService = new ContentManagementService(
            new ContentWriteRepository(dbContext),
            new LoggerConfiguration().CreateLogger());
        var service = CreateEventService(dbContext, contentService: contentService);
        dbContext.Contents.Add(new ContentEntity
        {
            PublicId = "content-123",
            Enabled = true,
            Versions =
            [
                new ContentVersionEntity
                {
                    VersionNumber = 2,
                    Content = "{\"title\":\"Current\"}",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2),
                    Published = true
                }
            ]
        });
        dbContext.Events.Add(CreatePendingEntity(
            CreateEvent(EventType.Delete, version: 2),
            DateTimeOffset.UtcNow.AddMinutes(-1)));
        await dbContext.SaveChangesAsync();

        await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        dbContext.Events.Add(CreatePendingEntity(
            CreateEvent(EventType.Publish, version: 1),
            DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
        var staleEventResult = await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        staleEventResult.IsSuccess.ShouldBeTrue();
        (await dbContext.Contents.CountAsync()).ShouldBe(0);
        var processedEvents = await dbContext.Events
            .Where(entity => entity.Status == EventStatus.Processed)
            .CountAsync();
        processedEvents.ShouldBe(2);
    }

    [Fact]
    public async Task ProcessEventsAsync_LogsProcessingFailures()
    {
        await using var dbContext = CreateDbContext();
        var sink = new TestLogEventSink();
        var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();
        var contentService = new RecordingContentManagementService { ShouldFail = true };
        var service = CreateEventService(dbContext, contentService: contentService, logger: logger);
        dbContext.Events.Add(CreatePendingEntity());
        await dbContext.SaveChangesAsync();

        await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        sink.Events.ShouldContain(logEvent =>
            logEvent.Level == LogEventLevel.Error &&
            logEvent.RenderMessage().Contains("Failed to update content from event"));
    }

    [Theory]
    [InlineData(0, EventStatus.Error, 1)]
    [InlineData(2, EventStatus.Quarantined, 3)]
    public async Task ProcessEventsAsync_TracksFailuresAndQuarantinesAtTheMaximumAttempts(
        int previousAttempts,
        EventStatus expectedStatus,
        int expectedAttempts)
    {
        await using var dbContext = CreateDbContext();
        var contentService = new RecordingContentManagementService
        {
            ShouldFail = true
        };
        var service = CreateEventService(dbContext, contentService: contentService);
        dbContext.Events.Add(CreatePendingEntity(processingAttempts: previousAttempts));
        await dbContext.SaveChangesAsync();

        var result = await service.ProcessEventsAsync(CreateProcessArgs(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var processedEvent = await dbContext.Events.SingleAsync();
        processedEvent.Status.ShouldBe(expectedStatus);
        processedEvent.ProcessingAttempts.ShouldBe(expectedAttempts);
        processedEvent.ErrorMessage.ShouldBe("processing failed");
    }

    private static CmsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CmsDbContext(options);
    }

    private static EventService CreateEventService(
        CmsDbContext dbContext,
        int maxBatchSize = 100,
        IContentManagementService? contentService = null,
        Serilog.ILogger? logger = null)
    {
        IEventWriteRepository writeRepository = new EventWriteRepository(dbContext);
        IEventReadRepository readRepository = new EventReadRepository(dbContext);

        return new EventService(
            writeRepository,
            readRepository,
            contentService ?? new RecordingContentManagementService(),
            Microsoft.Extensions.Options.Options.Create(
                new EventIngestionOptions { MaxBatchSize = maxBatchSize }),
            logger ?? new LoggerConfiguration().CreateLogger());
    }

    private static CmsEventModel CreateEvent(
        EventType type = EventType.Publish,
        int? version = 1,
        string publicId = "content-123") => new()
    {
        PublicId = publicId,
        Type = type,
        Payload = type is EventType.Delete or EventType.Unpublish ? null : new { title = "Example" },
        Version = version,
        Timestamp = DateTimeOffset.UtcNow
    };

    private static EventEntity CreatePendingEntity(
        int processingAttempts = 0,
        string publicId = "content-123") =>
        CreatePendingEntity(CreateEvent(publicId: publicId), DateTimeOffset.UtcNow, processingAttempts);

    private static EventEntity CreatePendingEntity(
        CmsEventModel cmsEvent,
        DateTimeOffset createdDateTime,
        int processingAttempts = 0) => new()
    {
        Payload = JsonSerializer.Serialize(cmsEvent),
        Status = EventStatus.Pending,
        ProcessingAttempts = processingAttempts,
        ErrorMessage = string.Empty,
        CreatedDateTime = createdDateTime
    };

    private static ProcessEventsArgs CreateProcessArgs() => new()
    {
        BatchSize = 10,
        ErrorEventsRatio = 0,
        ErrorCutoffTimeInMinutes = 10,
        MaxProcessingAttempts = 3
    };

    private sealed class RecordingContentManagementService : IContentManagementService
    {
        public List<CmsEventModel> Events { get; } = [];
        public bool ShouldFail { get; init; }

        public Task<Result> ExecuteContentChangeAsync(
            CmsEventModel cmsEvent,
            CancellationToken cancellationToken)
        {
            Events.Add(cmsEvent);
            return Task.FromResult(ShouldFail
                ? Result.Fail("processing failed")
                : Result.Ok());
        }
    }

    private sealed class TestLogEventSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
