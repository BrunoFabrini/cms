using CMS.Application.Services;
using CMS.Domain.Enums;
using CMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CMS.Infrastructure.Services;

public class AuthService(
    IHttpContextAccessor httpContextAccessor
) : IAuthService
{
    public ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public bool UserIsAdmin()
    {
        return User.IsInRole(MapRoleToClaim(UserRole.Admin));
    }

    protected static string MapRoleToClaim(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => AuthConstants.AdminRoleClaim,
            UserRole.User => AuthConstants.UserRoleClaim,
            UserRole.Service => AuthConstants.ServiceRoleClaim,
            _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unsupported user role: {role}")
        };
    }
}
