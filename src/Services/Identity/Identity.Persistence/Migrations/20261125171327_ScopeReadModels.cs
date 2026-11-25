using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Name",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_TenantId_Name",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Branches_OrganizationId_Name",
                table: "Branches");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_TenantId_Name",
                table: "Organizations",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_OrganizationId_Name",
                table: "Branches",
                columns: new[] { "OrganizationId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Name",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_TenantId_Name",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Branches_OrganizationId_Name",
                table: "Branches");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_TenantId_Name",
                table: "Organizations",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_OrganizationId_Name",
                table: "Branches",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }
    }
}
