using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurveilWin.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TopApps = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    IdleSeconds = table.Column<int>(type: "integer", nullable: false),
                    ActiveSeconds = table.Column<int>(type: "integer", nullable: false),
                    WindowTitles = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ProductivityScore = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "free"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "org_policies",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaptureFps = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false, defaultValue: 1.0m),
                    EnableOcr = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableScreenshots = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ScreenshotIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    ScreenshotRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    SummaryRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    AllowedApps = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    DeniedApps = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ExpectedShiftHours = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 8.0m),
                    AutoCloseShiftAfterHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    EnableAiSummaries = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AiProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "ollama"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_org_policies", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_org_policies_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    InviteToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InviteExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalActiveSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalIdleSeconds = table.Column<int>(type: "integer", nullable: false),
                    ShiftStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShiftEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TopApps = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    AiNarrative = table.Column<string>(type: "text", nullable: true),
                    AiModelUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AiGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductivityScore = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_summaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_summaries_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manager_assignments",
                columns: table => new
                {
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manager_assignments", x => new { x.ManagerId, x.EmployeeId });
                    table.ForeignKey(
                        name: "FK_manager_assignments_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manager_assignments_users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedHours = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 8.0m),
                    ActualHours = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AgentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shifts_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shifts_users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalFrames = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activity_sessions_shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_frames",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveApp = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    WindowTitle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AppCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsIdle = table.Column<bool>(type: "boolean", nullable: false),
                    IdleReason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OcrText = table.Column<string>(type: "text", nullable: true),
                    BrowserDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MonitorIndex = table.Column<short>(type: "smallint", nullable: true),
                    CursorX = table.Column<int>(type: "integer", nullable: true),
                    CursorY = table.Column<int>(type: "integer", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_frames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activity_frames_activity_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "activity_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_frames_EmployeeId_CapturedAt",
                table: "activity_frames",
                columns: new[] { "EmployeeId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_activity_frames_OrganizationId_CapturedAt",
                table: "activity_frames",
                columns: new[] { "OrganizationId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_activity_frames_SessionId",
                table: "activity_frames",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_sessions_SessionKey",
                table: "activity_sessions",
                column: "SessionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_sessions_ShiftId",
                table: "activity_sessions",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_summaries_EmployeeId_WindowStart",
                table: "activity_summaries",
                columns: new[] { "EmployeeId", "WindowStart" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_OrganizationId_CreatedAt",
                table: "audit_logs",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_summaries_EmployeeId_Date",
                table: "daily_summaries",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_summaries_OrganizationId_Date",
                table: "daily_summaries",
                columns: new[] { "OrganizationId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_manager_assignments_EmployeeId",
                table: "manager_assignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Slug",
                table: "organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shifts_Date",
                table: "shifts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_EmployeeId",
                table: "shifts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_OrganizationId",
                table: "shifts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_users_OrganizationId_Email",
                table: "users",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_frames");

            migrationBuilder.DropTable(
                name: "activity_summaries");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "daily_summaries");

            migrationBuilder.DropTable(
                name: "manager_assignments");

            migrationBuilder.DropTable(
                name: "org_policies");

            migrationBuilder.DropTable(
                name: "activity_sessions");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
