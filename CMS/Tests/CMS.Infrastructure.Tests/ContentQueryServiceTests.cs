using CMS.Application.Features.Contents;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Repositories.Content;
using CMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CMS.Infrastructure.Tests;

public class ContentQueryServiceTests
{
    [Fact]
    public async Task GetContentsAsync_UserSeesEnabledPublishedContentsOnly()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentsAsync(dbContext);
        var service = CreateService(dbContext, isAdmin: false);

        var result = await service.GetContentsAsync(new ListContentsRequest(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.Select(item => item.Id).ShouldBe(["visible"]);
    }

    [Fact]
    public async Task GetContentsAsync_AdminAlsoSeesDisabledContents()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentsAsync(dbContext);
        var service = CreateService(dbContext, isAdmin: true);

        var result = await service.GetContentsAsync(new ListContentsRequest(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Select(item => item.Id).ShouldBe(["disabled", "visible"]);
    }

    [Fact]
    public async Task GetContentsAsync_UsesPaginationAndRequestedOrdering()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Contents.AddRange(
            CreateContent("c", 1, enabled: true),
            CreateContent("a", 3, enabled: true),
            CreateContent("b", 2, enabled: true));
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, isAdmin: false);

        var firstPage = await service.GetContentsAsync(
            new ListContentsRequest
            {
                Page = 1,
                PageSize = 2,
                SortBy = "id",
                SortDirection = "desc"
            },
            CancellationToken.None);
        var secondPage = await service.GetContentsAsync(
            new ListContentsRequest
            {
                Page = 2,
                PageSize = 2,
                SortBy = "id",
                SortDirection = "desc"
            },
            CancellationToken.None);

        firstPage.IsSuccess.ShouldBeTrue();
        firstPage.Value.Items.Select(item => item.Id).ShouldBe(["c", "b"]);
        firstPage.Value.TotalPages.ShouldBe(2);
        secondPage.IsSuccess.ShouldBeTrue();
        secondPage.Value.Items.Select(item => item.Id).ShouldBe(["a"]);
    }

    [Fact]
    public async Task GetContentsAsync_HidesUnpublishedLatestVersions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Contents.Add(new ContentEntity
        {
            PublicId = "unpublished",
            Enabled = true,
            Versions =
            [
                new ContentVersionEntity
                {
                    VersionNumber = 1,
                    Content = "{\"title\":\"Hidden\"}",
                    Timestamp = DateTimeOffset.UtcNow,
                    Published = false
                }
            ]
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, isAdmin: true);

        var result = await service.GetContentsAsync(new ListContentsRequest(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentByIdAsync_ReturnsFailureWhenContentDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, isAdmin: false);

        var result = await service.GetContentByIdAsync("missing", CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldBe("Content not found.");
    }

    [Fact]
    public async Task GetContentByIdAsync_UserCannotReadDisabledContent()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentsAsync(dbContext);
        var service = CreateService(dbContext, isAdmin: false);

        var result = await service.GetContentByIdAsync("disabled", CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().Message.ShouldBe("Content not found.");
    }

    [Fact]
    public async Task GetContentByIdAsync_AdminCanReadDisabledContent()
    {
        await using var dbContext = CreateDbContext();
        await SeedContentsAsync(dbContext);
        var service = CreateService(dbContext, isAdmin: true);

        var result = await service.GetContentByIdAsync("disabled", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe("disabled");
    }

    private static ContentQueryService CreateService(CmsDbContext dbContext, bool isAdmin) =>
        new(new ContentReadRepository(dbContext), new TestAuthService(isAdmin));

    private static CmsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CmsDbContext(options);
    }

    private static async Task SeedContentsAsync(CmsDbContext dbContext)
    {
        dbContext.Contents.AddRange(
            CreateContent("visible", 1, enabled: true),
            CreateContent("disabled", 1, enabled: false));
        await dbContext.SaveChangesAsync();
    }

    private static ContentEntity CreateContent(string publicId, int version, bool enabled) =>
        new()
        {
            PublicId = publicId,
            Enabled = enabled,
            Versions =
            [
                new ContentVersionEntity
                {
                    VersionNumber = version,
                    Content = $"{{\"title\":\"{publicId}\"}}",
                    Timestamp = DateTimeOffset.UtcNow,
                    Published = true
                }
            ]
        };

    private sealed class TestAuthService(bool isAdmin) : IAuthService
    {
        public bool UserIsAdmin() => isAdmin;
    }
}
