using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantManagementDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Tenants",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfigurationJson",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Tenants",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Tenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Tenants",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndsAtUtc",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartsAtUtc",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Tenants] SET [Code] = UPPER(LEFT(REPLACE(CONVERT(varchar(36), [Id]), '-', ''), 32)), [ConfigurationJson] = '{}', [StartsAtUtc] = [CreatedAtUtc]");

            migrationBuilder.AlterColumn<string>(name: "Code", table: "Tenants", type: "nvarchar(32)", maxLength: 32, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(32)", oldMaxLength: 32, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ConfigurationJson", table: "Tenants", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<DateTimeOffset>(name: "StartsAtUtc", table: "Tenants", type: "datetimeoffset", nullable: false, oldClrType: typeof(DateTimeOffset), oldType: "datetimeoffset", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Code",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ConfigurationJson",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EndsAtUtc",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StartsAtUtc",
                table: "Tenants");
        }
    }
}
