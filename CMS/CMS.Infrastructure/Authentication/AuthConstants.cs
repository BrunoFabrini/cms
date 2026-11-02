namespace CMS.Infrastructure.Authentication;

public static class AuthConstants
{
    public const string ServiceClient = "ServiceClient";
    public const string UserClient = "UserClient";
    public const string AdministratorPolicy = "ApiAdministrator";
    public const string UsersConfigurationSection = "Authentication:Users";
    public const string AdminRoleClaim = "admin";
    public const string UserRoleClaim = "user";
    public const string ServiceRoleClaim = "service";
}
