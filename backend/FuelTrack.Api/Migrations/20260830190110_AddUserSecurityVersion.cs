using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurityVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecurityVersion",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                table: "Usuarios");
        }
    }
}
