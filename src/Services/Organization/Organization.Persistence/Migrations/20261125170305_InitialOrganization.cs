using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.UniqueConstraint("AK_Tenants_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Tenant_Self", "[TenantId] = [Id]");
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.UniqueConstraint("AK_Organizations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Organizations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "organization",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.UniqueConstraint("AK_Branches_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.UniqueConstraint("AK_Branches_TenantId_OrganizationId_Id", x => new { x.TenantId, x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Branches_Organizations_TenantId_OrganizationId",
                        columns: x => new { x.TenantId, x.OrganizationId },
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Branches_TenantId_OrganizationId_BranchId",
                        columns: x => new { x.TenantId, x.OrganizationId, x.BranchId },
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumns: new[] { "TenantId", "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_TenantId_OrganizationId",
                        columns: x => new { x.TenantId, x.OrganizationId },
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointOfSales",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointOfSales", x => x.Id);
                    table.UniqueConstraint("AK_PointOfSales_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PointOfSales_Branches_TenantId_BranchId",
                        columns: x => new { x.TenantId, x.BranchId },
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.UniqueConstraint("AK_Warehouses_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Warehouses_Branches_TenantId_BranchId",
                        columns: x => new { x.TenantId, x.BranchId },
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_OrganizationId",
                schema: "organization",
                table: "Branches",
                columns: new[] { "TenantId", "OrganizationId" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_OrganizationId_Code",
                schema: "organization",
                table: "Branches",
                columns: new[] { "TenantId", "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_TenantId_OrganizationId_BranchId",
                schema: "organization",
                table: "OrganizationMembers",
                columns: new[] { "TenantId", "OrganizationId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_TenantId_OrganizationId_UserId_BranchId",
                schema: "organization",
                table: "OrganizationMembers",
                columns: new[] { "TenantId", "OrganizationId", "UserId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_TenantId_UserId",
                schema: "organization",
                table: "OrganizationMembers",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_TenantId_Code",
                schema: "organization",
                table: "Organizations",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PointOfSales_TenantId_BranchId_Code",
                schema: "organization",
                table: "PointOfSales",
                columns: new[] { "TenantId", "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                schema: "organization",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_BranchId",
                schema: "organization",
                table: "Warehouses",
                columns: new[] { "TenantId", "BranchId" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_BranchId_Code",
                schema: "organization",
                table: "Warehouses",
                columns: new[] { "TenantId", "BranchId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationMembers",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "PointOfSales",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Branches",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "organization");
        }
    }
}
