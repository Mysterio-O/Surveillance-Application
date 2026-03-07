using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;
using SurveilWin.Api.DTOs.Users;
using SurveilWin.Api.Extensions;
using SurveilWin.Api.Services;

namespace SurveilWin.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;

    public UsersController(AppDbContext db, IEmailService email, IAuditService audit, IConfiguration config)
    {
        _db = db; _email = email; _audit = audit; _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var orgId = User.GetOrgId();
        var users = await _db.Users
            .Where(u => u.OrganizationId == orgId)
            .OrderBy(u => u.LastName)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FirstName + " " + u.LastName,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                OrganizationId = u.OrganizationId,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var requestorId = User.GetUserId();
        var user = await _db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var requestor = await _db.Users.FindAsync(requestorId);
        if (requestor == null) return Forbid();
        if (requestor.Role != UserRole.SuperAdmin && user.OrganizationId != requestor.OrganizationId && requestorId != id)
            return Forbid();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            OrganizationId = user.OrganizationId,
            OrgName = user.Organization.Name,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("invite")]
    [Authorize(Roles = "OrgAdmin,Manager,SuperAdmin")]
    public async Task<IActionResult> Invite([FromBody] InviteUserRequest req)
    {
        var actorId = User.GetUserId();
        var orgId = User.GetOrgId();
        var actor = await _db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == actorId);
        if (actor == null) return Forbid();

        if (!Enum.TryParse<UserRole>(req.Role, out var role))
            return BadRequest(new { message = "Invalid role" });

        if (role < actor.Role)
            return Forbid();

        var exists = await _db.Users.AnyAsync(u => u.OrganizationId == orgId && u.Email == req.Email.ToLower());
        if (exists) return Conflict(new { message = "User with this email already exists in the organization" });

        var token = GenerateSecureToken();
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = req.Email.ToLower(),
            PasswordHash = "",
            FirstName = req.FirstName,
            LastName = req.LastName,
            Role = role,
            IsActive = false,
            InvitedBy = actorId,
            InviteToken = token,
            InviteExpires = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        if (req.ManagerId.HasValue)
        {
            _db.ManagerAssignments.Add(new ManagerAssignment
            {
                ManagerId = req.ManagerId.Value,
                EmployeeId = user.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        var dashUrl = _config["App:DashboardUrl"] ?? "http://localhost:5173";
        var inviteUrl = $"{dashUrl}/accept-invite?token={token}";
        await _email.SendInviteEmailAsync(user.Email, user.FirstName, actor.FullName, actor.Organization.Name, req.Role, inviteUrl);

        await _audit.LogAsync(orgId, actorId, "USER_INVITED", "User", user.Id.ToString());

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { id = user.Id });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var actorId = User.GetUserId();
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.OrganizationId, actorId, "USER_DEACTIVATED", "User", id.ToString());
        return NoContent();
    }

    [HttpPost("assignments")]
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> AssignManager([FromBody] AssignManagerRequest req)
    {
        var orgId = User.GetOrgId();
        var manager = await _db.Users.FindAsync(req.ManagerId);
        if (manager == null || manager.OrganizationId != orgId) return BadRequest(new { message = "Manager not found in org" });

        foreach (var empId in req.EmployeeIds)
        {
            var emp = await _db.Users.FindAsync(empId);
            if (emp == null || emp.OrganizationId != orgId) continue;

            var exists = await _db.ManagerAssignments.AnyAsync(ma => ma.ManagerId == req.ManagerId && ma.EmployeeId == empId);
            if (!exists)
            {
                _db.ManagerAssignments.Add(new ManagerAssignment
                {
                    ManagerId = req.ManagerId,
                    EmployeeId = empId,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("my-team")]
    [Authorize(Roles = "Manager,OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> GetMyTeam()
    {
        var managerId = User.GetUserId();
        var team = await _db.ManagerAssignments
            .Where(ma => ma.ManagerId == managerId)
            .Include(ma => ma.Employee)
            .Select(ma => new UserProfileDto
            {
                Id = ma.Employee.Id,
                Email = ma.Employee.Email,
                FirstName = ma.Employee.FirstName,
                LastName = ma.Employee.LastName,
                FullName = ma.Employee.FirstName + " " + ma.Employee.LastName,
                Role = ma.Employee.Role.ToString(),
                IsActive = ma.Employee.IsActive,
                OrganizationId = ma.Employee.OrganizationId
            })
            .ToListAsync();
        return Ok(team);
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest req)
    {
        var actorId = User.GetUserId();
        if (!Enum.TryParse<UserRole>(req.Role, out var role))
            return BadRequest(new { message = "Invalid role" });

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.OrganizationId, actorId, "USER_ROLE_CHANGED", "User", id.ToString());
        return Ok(new { id, role = req.Role });
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }
}
