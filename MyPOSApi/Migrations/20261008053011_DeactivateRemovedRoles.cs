using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPOSApi.Migrations
{
    /// <inheritdoc />
    public partial class DeactivateRemovedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");
        }
    }
}
