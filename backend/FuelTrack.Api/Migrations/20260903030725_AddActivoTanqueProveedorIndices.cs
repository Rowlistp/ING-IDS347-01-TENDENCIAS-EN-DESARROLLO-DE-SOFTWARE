using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddActivoTanqueProveedorIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Tanques",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Proveedores",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposCombustible_Nombre",
                table: "TiposCombustible",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Rnc",
                table: "Proveedores",
                column: "Rnc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TiposCombustible_Nombre",
                table: "TiposCombustible");

            migrationBuilder.DropIndex(
                name: "IX_Proveedores_Rnc",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Tanques");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Proveedores");
        }
    }
}
