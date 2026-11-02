using CMS.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CMS.Infrastructure.Authentication;

public class UserAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration
) : BaseAuthenticationHandler(options, logger, encoder, configuration)
{
    private readonly UserRole[] _allowedRoles = [UserRole.Admin, UserRole.User];

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var getCredentialsResult = GetCredentials();

        if (getCredentialsResult.IsFailed)
            return AuthenticateResult.Fail(getCredentialsResult.Errors[0].Message);

        try
        {
            var user = GetUser(
                username: getCredentialsResult.Value.Username,
                password: getCredentialsResult.Value.Password
            );

            if (user is null)
                return AuthenticateResult.Fail("Invalid service credentials.");

            if (!IsUserInRole(user, _allowedRoles))
                return AuthenticateResult.Fail("User does not have the required role.");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Credentials are not valid base64.");
        }
    }
}
