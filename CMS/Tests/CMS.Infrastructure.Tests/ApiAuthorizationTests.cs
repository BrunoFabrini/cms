using CMS.Api.Controllers;
using CMS.Application.Features.CmsEvents;
using CMS.Application.Models.Events;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Repositories.Content;
using CMS.Infrastructure.Authentication;
using CMS.Infrastructure.DependencyInjection;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Repositories.Content;
using CMS.Infrastructure.Services;
using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CMS.Infrastructure.Tests;

public class ApiAuthorizationTests
{
    [Fact]
    public async Task PostEvents_RejectsAnonymousRequests()
    {
        await using var host = await TestApiHost.CreateAsync();

        var response = await host.Client.PostAsync("/cms/events", JsonContent.Create(Array.Empty<CmsEventModel>()));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContents_ReturnsOnlyEnabledContentForARegularUser()
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/content/list");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.RegularUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        document.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("items").EnumerateArray().Single()
            .GetProperty("id").GetString().ShouldBe("visible");
    }

    [Fact]
    public async Task GetContents_ReturnsDisabledContentForAnAdministrator()
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/content/list");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.AdminUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        document.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(2);
        document.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .ShouldBe(["disabled", "visible"]);
    }

    [Fact]
    public async Task DisableContent_AllowsAdministrators()
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync();
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/content/visible/disable");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.AdminUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var storedContent = await host.GetContentAsync("visible");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        storedContent.Enabled.ShouldBeFalse();
    }

    [Fact]
    public async Task DisableContent_RejectsRegularUsers()
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync();
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/content/visible/disable");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.RegularUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var storedContent = await host.GetContentAsync("visible");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        storedContent.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task DisableContent_ChangesOnlyAdministrativeState()
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync();
        var before = await host.GetContentAsync("visible");
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/content/visible/disable");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.AdminUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var after = await host.GetContentAsync("visible");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        after.Enabled.ShouldBeFalse();
        after.Versions.Single().VersionNumber.ShouldBe(before.Versions.Single().VersionNumber);
        after.Versions.Single().Content.ShouldBe(before.Versions.Single().Content);
        after.Versions.Single().Timestamp.ShouldBe(before.Versions.Single().Timestamp);
    }

    [Theory]
    [InlineData("regular-user", "PUT")]
    [InlineData("admin-user01", "PUT")]
    public async Task ContentEndpoints_DoNotAllowDirectContentUpdates(
        string username,
        string method)
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/content/visible")
        {
            Content = JsonContent.Create(new { title = "Tampered" })
        };
        request.Headers.Authorization = BasicAuthorization(username, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var storedContent = await host.GetContentAsync("visible");

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        storedContent.Versions.Single().Content.ShouldBe("{\"title\":\"visible\"}");
    }

    [Fact]
    public async Task GetContents_AppliesPaginationAndOrdering()
    {
        await using var host = await TestApiHost.CreateAsync();
        await host.SeedContentsAsync("a", "b", "c");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/content/list?page=1&pageSize=2&sortBy=id&sortDirection=desc");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.RegularUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        document.RootElement.GetProperty("totalPages").GetInt32().ShouldBe(2);
        document.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString())
            .ShouldBe(["c", "b"]);
    }

    [Fact]
    public async Task GetContent_ReturnsBadRequestWhenContentDoesNotExist()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/content/missing");
        request.Headers.Authorization = BasicAuthorization(TestApiHost.RegularUsername, TestApiHost.Password);

        var response = await host.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void CmsEventsController_RequiresServiceAuthentication()
    {
        var authorize = typeof(CmsController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Single();

        authorize.AuthenticationSchemes.ShouldBe(AuthConstants.ServiceClient);
    }

    private static AuthenticationHeaderValue BasicAuthorization(string username, string password)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    private sealed class TestApiHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public const string RegularUsername = "regular-user";
        public const string AdminUsername = "admin-user01";
        public const string ServiceUsername = "webhook-service";
        public const string Password = "test-password";

        public HttpClient Client { get; } = client;

        public static async Task<TestApiHost> CreateAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AuthConstants.UsersConfigurationSection}:0:Username"] = RegularUsername,
                [$"{AuthConstants.UsersConfigurationSection}:0:Password"] = Password,
                [$"{AuthConstants.UsersConfigurationSection}:0:Role"] = "user",
                [$"{AuthConstants.UsersConfigurationSection}:1:Username"] = AdminUsername,
                [$"{AuthConstants.UsersConfigurationSection}:1:Password"] = Password,
                [$"{AuthConstants.UsersConfigurationSection}:1:Role"] = "admin",
                [$"{AuthConstants.UsersConfigurationSection}:2:Username"] = ServiceUsername,
                [$"{AuthConstants.UsersConfigurationSection}:2:Password"] = Password,
                [$"{AuthConstants.UsersConfigurationSection}:2:Role"] = "service"
            });

            var databaseName = Guid.NewGuid().ToString();
            builder.Services.AddControllers().AddApplicationPart(typeof(ContentController).Assembly);
            builder.Services.AddDbContext<CmsDbContext>(options => options.UseInMemoryDatabase(databaseName));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAppAuthentication();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEventService, SuccessfulEventService>();
            builder.Services.AddScoped<IContentQueryService, ContentQueryService>();
            builder.Services.AddScoped<IContentAdministrationService, ContentAdministrationService>();
            builder.Services.AddScoped<IContentReadRepository, ContentReadRepository>();
            builder.Services.AddScoped<IContentWriteRepository, ContentWriteRepository>();

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();

            return new TestApiHost(app, app.GetTestClient());
        }

        public async Task SeedContentsAsync(params string[] publicIds)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            if (publicIds.Length == 0)
            {
                dbContext.Contents.AddRange(
                    CreateContent("visible", enabled: true),
                    CreateContent("disabled", enabled: false));
            }
            else
            {
                dbContext.Contents.AddRange(publicIds.Select(id => CreateContent(id, enabled: true)));
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task<ContentEntity> GetContentAsync(string publicId)
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<CmsDbContext>()
                .Contents.Include(content => content.Versions)
                .SingleAsync(content => content.PublicId == publicId);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }

        private static ContentEntity CreateContent(string id, bool enabled) =>
            new()
            {
                PublicId = id,
                Enabled = enabled,
                Versions =
                [
                    new ContentVersionEntity
                    {
                        VersionNumber = 1,
                        Content = $"{{\"title\":\"{id}\"}}",
                        Timestamp = DateTimeOffset.UtcNow,
                        Published = true
                    }
                ]
            };
    }

    private sealed class SuccessfulEventService : IEventService
    {
        public Task<Result> IngestEventsAsync(
            PostEventsRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Ok());

        public Task<Result> ProcessEventsAsync(
            CMS.Application.Models.Events.ProcessEventsArgs args,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Ok());
    }
}
