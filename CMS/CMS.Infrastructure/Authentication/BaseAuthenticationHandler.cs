using CMS.Application.Models.Users;
using CMS.Domain.Enums;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;

namespace CMS.Infrastructure.Authentication;

public abstract class BaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected Result<(string Username, string Password)> GetCredentials()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            return Result.Fail("Missing Authorization header.");

        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var headerValue) ||
            !string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(headerValue.Parameter))
        {
            return Result.Fail("Invalid Authorization scheme.");
        }

        var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter)).Split(':', 2);

        if (credentials.Length != 2)
            return Result.Fail("Invalid credentials format.");

        return Result.Ok((credentials[0], credentials[1]));
    }

    protected UserModel? GetUser(string username, string password)
    {
        var user = configuration.GetSection(AuthConstants.UsersConfigurationSection)
                .GetChildren()
                .FirstOrDefault(candidate =>
                    string.Equals(candidate["Username"], username, StringComparison.Ordinal) &&
                    string.Equals(candidate["Password"], password, StringComparison.Ordinal));

        return user is null
            ? null
            : new UserModel
            {
                Username = user["Username"] ?? string.Empty,
                Password = user["Password"] ?? string.Empty,
                Role = user["Role"] ?? string.Empty
            };
    }

    protected static bool IsUserInRole(UserModel user, UserRole[] allowedRoles)
    {
        var role = Enum.Parse<UserRole>(user.Role, ignoreCase: true);
        return allowedRoles.Contains(role);
    }
}
