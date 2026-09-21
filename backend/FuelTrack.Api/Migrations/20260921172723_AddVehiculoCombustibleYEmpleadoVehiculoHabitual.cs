using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiculoCombustibleYEmpleadoVehiculoHabitual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoCombustibleId",
                table: "Vehiculos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehiculoHabitualId",
                table: "Empleados",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_TipoCombustibleId",
                table: "Vehiculos",
                column: "TipoCombustibleId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_VehiculoHabitualId",
                table: "Empleados",
                column: "VehiculoHabitualId");

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_Vehiculos_VehiculoHabitualId",
                table: "Empleados",
                column: "VehiculoHabitualId",
                principalTable: "Vehiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehiculos_TiposCombustible_TipoCombustibleId",
                table: "Vehiculos",
                column: "TipoCombustibleId",
                principalTable: "TiposCombustible",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Vehiculos_VehiculoHabitualId",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehiculos_TiposCombustible_TipoCombustibleId",
                table: "Vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_Vehiculos_TipoCombustibleId",
                table: "Vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_VehiculoHabitualId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoCombustibleId",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "VehiculoHabitualId",
                table: "Empleados");
        }
    }
}
