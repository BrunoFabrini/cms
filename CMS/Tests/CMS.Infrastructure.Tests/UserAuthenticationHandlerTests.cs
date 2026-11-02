using CMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Text;
using System.Text.Encodings.Web;

namespace CMS.Infrastructure.Tests;

public class UserAuthenticationHandlerTests
{
    [Theory]
    [InlineData("alice-user1", "correct-password", true)]
    [InlineData("alice-user1", "wrong-password", false)]
    [InlineData("unknown-user", "correct-password", false)]
    [InlineData("unknown-user", "wrong-password", false)]
    public async Task AuthenticateAsync_ValidatesConfiguredUsernameAndPassword(
        string username,
        string password,
        bool expectedSuccess)
    {
        var result = await AuthenticateAsync(username, password);

        result.Succeeded.ShouldBe(expectedSuccess);
        if (expectedSuccess)
        {
            result.Principal!.Identity!.Name.ShouldBe("alice-user1");
        }
        else
        {
            result.Failure.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task AuthenticateAsync_RejectsMissingCredentials()
    {
        var result = await AuthenticateAsync();

        result.Succeeded.ShouldBeFalse();
        result.Failure!.Message.ShouldContain("Missing Authorization header");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("username-is-longer-than-twenty")]
    public async Task AuthenticateAsync_RejectsUsernamesOutsideTheAllowedLength(
        string username)
    {
        var result = await AuthenticateAsync(username, "correct-password");

        result.Succeeded.ShouldBeFalse();
        result.Failure!.Message.ShouldContain("between 10 and 20 characters");
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(
        string? username = null,
        string? password = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AuthConstants.UsersConfigurationSection}:0:Username"] = "alice-user1",
                [$"{AuthConstants.UsersConfigurationSection}:0:Password"] = "correct-password",
                [$"{AuthConstants.UsersConfigurationSection}:0:Role"] = "User"
            })
            .Build();
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var handler = new UserAuthenticationHandler(
            new TestOptionsMonitor<AuthenticationSchemeOptions>(new()),
            loggerFactory,
            UrlEncoder.Default,
            configuration);
        var context = new DefaultHttpContext();

        if (username is not null && password is not null)
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            context.Request.Headers.Authorization = $"Basic {credentials}";
        }

        await handler.InitializeAsync(
            new AuthenticationScheme(AuthConstants.UserClient, null, typeof(UserAuthenticationHandler)),
            context);

        return await handler.AuthenticateAsync();
    }

    private sealed class TestOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        public TOptions CurrentValue => value;

        public TOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
