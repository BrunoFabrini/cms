using CMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure.DependencyInjection;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ServiceAuthenticationHandler>(AuthConstants.ServiceClient, _ => { })
            .AddScheme<AuthenticationSchemeOptions, UserAuthenticationHandler>(AuthConstants.UserClient, _ => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.AdministratorPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(AuthConstants.UserClient);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthConstants.AdminRoleClaim);
            });

        return services;
    }
}
