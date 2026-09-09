using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudesRecurrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesRecurrentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CantidadSolicitada = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Periodicidad = table.Column<string>(type: "text", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaEjecucion = table.Column<DateOnly>(type: "date", nullable: true),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    VehiculoId = table.Column<int>(type: "integer", nullable: false),
                    DepartamentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoCombustibleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesRecurrentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesRecurrentes_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesRecurrentes_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesRecurrentes_TiposCombustible_TipoCombustibleId",
                        column: x => x.TipoCombustibleId,
                        principalTable: "TiposCombustible",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesRecurrentes_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRecurrentes_DepartamentoId",
                table: "SolicitudesRecurrentes",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRecurrentes_EmpleadoId",
                table: "SolicitudesRecurrentes",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRecurrentes_TipoCombustibleId",
                table: "SolicitudesRecurrentes",
                column: "TipoCombustibleId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRecurrentes_VehiculoId",
                table: "SolicitudesRecurrentes",
                column: "VehiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesRecurrentes");
        }
    }
}
