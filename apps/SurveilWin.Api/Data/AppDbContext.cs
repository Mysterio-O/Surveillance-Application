using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ManagerAssignment> ManagerAssignments => Set<ManagerAssignment>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ActivitySession> ActivitySessions => Set<ActivitySession>();
    public DbSet<ActivityFrame> ActivityFrames => Set<ActivityFrame>();
    public DbSet<ActivitySummary> ActivitySummaries => Set<ActivitySummary>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<OrgPolicy> OrgPolicies => Set<OrgPolicy>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Organizations
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(o => o.Slug).IsUnique();
            e.Property(o => o.Plan).HasMaxLength(50).HasDefaultValue("free");
            e.Property(o => o.IsActive).HasDefaultValue(true);
            e.Property(o => o.CreatedAt).HasDefaultValueSql("NOW()");
            e.Property(o => o.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // Users
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Email).HasMaxLength(320).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(u => u.IsActive).HasDefaultValue(true);
            e.Property(u => u.InviteToken).HasMaxLength(200);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            e.Property(u => u.UpdatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(u => new { u.OrganizationId, u.Email }).IsUnique();
            e.HasIndex(u => u.Email);
            e.Ignore(u => u.FullName);
            e.HasOne(u => u.Organization).WithMany(o => o.Users).HasForeignKey(u => u.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // ManagerAssignments
        modelBuilder.Entity<ManagerAssignment>(e =>
        {
            e.ToTable("manager_assignments");
            e.HasKey(ma => new { ma.ManagerId, ma.EmployeeId });
            e.Property(ma => ma.AssignedAt).HasDefaultValueSql("NOW()");
            e.HasOne(ma => ma.Manager).WithMany().HasForeignKey(ma => ma.ManagerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ma => ma.Employee).WithMany().HasForeignKey(ma => ma.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        // Shifts
        modelBuilder.Entity<Shift>(e =>
        {
            e.ToTable("shifts");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.ExpectedHours).HasPrecision(4, 2).HasDefaultValue(8.0m);
            e.Property(s => s.ActualHours).HasPrecision(4, 2);
            e.Property(s => s.AgentVersion).HasMaxLength(50);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(s => s.EmployeeId);
            e.HasIndex(s => s.Date);
            e.HasOne(s => s.Employee).WithMany().HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Organization).WithMany(o => o.Shifts).HasForeignKey(s => s.OrganizationId);
        });

        // ActivitySessions
        modelBuilder.Entity<ActivitySession>(e =>
        {
            e.ToTable("activity_sessions");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.SessionKey).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.SessionKey).IsUnique();
            e.Property(s => s.TotalFrames).HasDefaultValue(0);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(s => s.Shift).WithMany(sh => sh.Sessions).HasForeignKey(s => s.ShiftId).OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityFrames
        modelBuilder.Entity<ActivityFrame>(e =>
        {
            e.ToTable("activity_frames");
            e.HasKey(f => f.Id);
            e.Property(f => f.ActiveApp).HasMaxLength(260).IsRequired();
            e.Property(f => f.WindowTitle).HasMaxLength(1000).IsRequired();
            e.Property(f => f.AppCategory).HasMaxLength(50);
            e.Property(f => f.IdleReason).HasMaxLength(30);
            e.Property(f => f.BrowserDomain).HasMaxLength(255);
            e.Property(f => f.ThumbnailPath).HasMaxLength(500);
            e.Property(f => f.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(f => new { f.SessionId });
            e.HasIndex(f => new { f.EmployeeId, f.CapturedAt });
            e.HasIndex(f => new { f.OrganizationId, f.CapturedAt });
            e.HasOne(f => f.Session).WithMany(s => s.Frames).HasForeignKey(f => f.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ActivitySummaries
        modelBuilder.Entity<ActivitySummary>(e =>
        {
            e.ToTable("activity_summaries");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.TopApps).HasColumnType("jsonb");
            e.Property(s => s.WindowTitles).HasColumnType("jsonb");
            e.Property(s => s.ProductivityScore).HasPrecision(4, 2);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(s => new { s.EmployeeId, s.WindowStart });
        });

        // DailySummaries
        modelBuilder.Entity<DailySummary>(e =>
        {
            e.ToTable("daily_summaries");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(d => d.TopApps).HasColumnType("jsonb");
            e.Property(d => d.ProductivityScore).HasPrecision(4, 2);
            e.Property(d => d.AiNarrative).HasColumnType("text");
            e.Property(d => d.AiModelUsed).HasMaxLength(100);
            e.Property(d => d.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(d => new { d.EmployeeId, d.Date }).IsUnique();
            e.HasIndex(d => new { d.OrganizationId, d.Date });
            e.HasOne(d => d.Employee).WithMany().HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        // OrgPolicy
        modelBuilder.Entity<OrgPolicy>(e =>
        {
            e.ToTable("org_policies");
            e.HasKey(p => p.OrganizationId);
            e.Property(p => p.CaptureFps).HasPrecision(4, 1).HasDefaultValue(1.0m);
            e.Property(p => p.EnableOcr).HasDefaultValue(true);
            e.Property(p => p.EnableScreenshots).HasDefaultValue(false);
            e.Property(p => p.AllowedApps).HasColumnType("jsonb");
            e.Property(p => p.DeniedApps).HasColumnType("jsonb");
            e.Property(p => p.ExpectedShiftHours).HasPrecision(4, 2).HasDefaultValue(8.0m);
            e.Property(p => p.AutoCloseShiftAfterHours).HasDefaultValue(12);
            e.Property(p => p.EnableAiSummaries).HasDefaultValue(true);
            e.Property(p => p.AiProvider).HasMaxLength(50).HasDefaultValue("ollama");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(p => p.Organization).WithOne(o => o.Policy).HasForeignKey<OrgPolicy>(p => p.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.ResourceType).HasMaxLength(50);
            e.Property(a => a.ResourceId).HasMaxLength(100);
            e.Property(a => a.IpAddress).HasMaxLength(45);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(a => new { a.OrganizationId, a.CreatedAt });
        });
    }
}
