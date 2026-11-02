namespace CMS.Application.Models.Users;

public class UserModel
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Role { get; set; } = default!;
}