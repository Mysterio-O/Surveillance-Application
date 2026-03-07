using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrganizationsController(AppDbContext db) { _db = db; }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateOrgRequest req)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Slug = req.Name.ToLower().Replace(" ", "-"),
            Plan = "free",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);
        _db.OrgPolicies.Add(new OrgPolicy { OrganizationId = org.Id, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = org.Id }, org);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var org = await _db.Organizations.Include(o => o.Policy).FirstOrDefaultAsync(o => o.Id == id);
        if (org == null) return NotFound();
        return Ok(org);
    }

    [HttpGet("{id}/policy")]
    public async Task<IActionResult> GetPolicy(Guid id)
    {
        var policy = await _db.OrgPolicies.FindAsync(id);
        if (policy == null) return NotFound();
        return Ok(policy);
    }

    [HttpPut("{id}/policy")]
    [Authorize(Roles = "OrgAdmin,SuperAdmin")]
    public async Task<IActionResult> UpdatePolicy(Guid id, [FromBody] UpdatePolicyRequest req)
    {
        var policy = await _db.OrgPolicies.FindAsync(id);
        if (policy == null) return NotFound();

        if (req.CaptureFps.HasValue) policy.CaptureFps = req.CaptureFps.Value;
        if (req.EnableOcr.HasValue) policy.EnableOcr = req.EnableOcr.Value;
        if (req.EnableScreenshots.HasValue) policy.EnableScreenshots = req.EnableScreenshots.Value;
        if (req.ScreenshotIntervalMinutes.HasValue) policy.ScreenshotIntervalMinutes = req.ScreenshotIntervalMinutes.Value;
        if (req.ExpectedShiftHours.HasValue) policy.ExpectedShiftHours = req.ExpectedShiftHours.Value;
        if (req.AutoCloseShiftAfterHours.HasValue) policy.AutoCloseShiftAfterHours = req.AutoCloseShiftAfterHours.Value;
        if (req.EnableAiSummaries.HasValue) policy.EnableAiSummaries = req.EnableAiSummaries.Value;
        if (req.AiProvider != null) policy.AiProvider = req.AiProvider;
        policy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(policy);
    }

    [HttpGet("{id}/policy/agent-config")]
    [Authorize]
    public async Task<IActionResult> GetAgentConfig(Guid id)
    {
        var policy = await _db.OrgPolicies.FindAsync(id);
        if (policy == null) return Ok(new { captureFps = 1.0, enableOcr = true, enableScreenshots = false });

        var allowedApps = policy.AllowedApps.RootElement.Deserialize<List<string>>() ?? new();
        var deniedApps = policy.DeniedApps.RootElement.Deserialize<List<string>>() ?? new();

        return Ok(new
        {
            captureFps = (double)policy.CaptureFps,
            enableOcr = policy.EnableOcr,
            enableScreenshots = policy.EnableScreenshots,
            screenshotIntervalMinutes = policy.ScreenshotIntervalMinutes,
            allowedApps,
            deniedApps,
            expectedShiftHours = (double)policy.ExpectedShiftHours
        });
    }
}

public class CreateOrgRequest { public string Name { get; set; } = ""; }

public class UpdatePolicyRequest
{
    public decimal? CaptureFps { get; set; }
    public bool? EnableOcr { get; set; }
    public bool? EnableScreenshots { get; set; }
    public int? ScreenshotIntervalMinutes { get; set; }
    public decimal? ExpectedShiftHours { get; set; }
    public int? AutoCloseShiftAfterHours { get; set; }
    public bool? EnableAiSummaries { get; set; }
    public string? AiProvider { get; set; }
}
