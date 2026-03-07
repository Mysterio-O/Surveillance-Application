namespace SurveilWin.Api.DTOs.Auth;

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public Guid OrganizationId { get; set; }
    public string? OrgName { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = "";
}

public class AcceptInviteRequest
{
    public string Token { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
