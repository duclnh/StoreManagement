using Identity.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Identity.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261122000100_AddAuditScope")]
public sealed partial class AddAuditScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PreviousRefreshTokenHash", table: "LoginSessions", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProtectedRefreshToken", table: "LoginSessions", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "RefreshRetryUntilUtc", table: "LoginSessions", type: "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ScopeId", table: "SecurityAuditEvents", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_SecurityAuditEvents_TenantId_ScopeId_OccurredAtUtc",
            table: "SecurityAuditEvents", columns: new[] { "TenantId", "ScopeId", "OccurredAtUtc" });
        // Historical events have no reliable organization/branch attribution.
        // Keep them visible at tenant scope only.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PreviousRefreshTokenHash", table: "LoginSessions");
        migrationBuilder.DropColumn(name: "ProtectedRefreshToken", table: "LoginSessions");
        migrationBuilder.DropColumn(name: "RefreshRetryUntilUtc", table: "LoginSessions");
        migrationBuilder.DropIndex(name: "IX_SecurityAuditEvents_TenantId_ScopeId_OccurredAtUtc", table: "SecurityAuditEvents");
        migrationBuilder.DropColumn(name: "ScopeId", table: "SecurityAuditEvents");
    }
}
