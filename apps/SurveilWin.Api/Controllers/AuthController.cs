using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.DTOs.Auth;
using SurveilWin.Api.Services;

namespace SurveilWin.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, AppDbContext db, IEmailService email, IConfiguration config)
    {
        _auth = auth; _db = db; _email = email; _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req.Email, req.Password);
        if (result == null) return Unauthorized(new { message = "Invalid email or password" });
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req)
    {
        var result = await _auth.RefreshTokenAsync(req.RefreshToken);
        if (result == null) return Unauthorized(new { message = "Invalid refresh token" });
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new { message = "Logged out" });

    [HttpPost("accept-invite")]
    public async Task<ActionResult<AuthResponse>> AcceptInvite([FromBody] AcceptInviteRequest req)
    {
        if (req.Password != req.ConfirmPassword)
            return BadRequest(new { message = "Passwords do not match" });

        if (!IsValidPassword(req.Password))
            return BadRequest(new { message = "Password must be at least 8 characters with uppercase, lowercase, digit, and special character" });

        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.InviteToken == req.Token && u.InviteExpires > DateTime.UtcNow);

        if (user == null) return BadRequest(new { message = "Invalid or expired invite token" });

        user.PasswordHash = _auth.HashPassword(req.Password);
        user.IsActive = true;
        user.InviteToken = null;
        user.InviteExpires = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _auth.GenerateJwtToken(user);
        return Ok(new AuthResponse
        {
            AccessToken = token,
            RefreshToken = _auth.GenerateRefreshToken(),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                OrganizationId = user.OrganizationId,
                OrgName = user.Organization.Name
            }
        });
    }

    private static bool IsValidPassword(string pw) =>
        pw.Length >= 8 &&
        pw.Any(char.IsUpper) &&
        pw.Any(char.IsLower) &&
        pw.Any(char.IsDigit) &&
        pw.Any(c => !char.IsLetterOrDigit(c));
}
