using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCierreDiarioDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActaDigital",
                table: "CierresDiarios");

            migrationBuilder.DropColumn(
                name: "ReporteUrl",
                table: "CierresDiarios");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreadoEn",
                table: "CierresDiarios",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "CierresDiarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "PdfActa",
                table: "CierresDiarios",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalDespachos",
                table: "CierresDiarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CierresDiariosDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CierreDiarioId = table.Column<int>(type: "integer", nullable: false),
                    TanqueId = table.Column<int>(type: "integer", nullable: false),
                    NumeroDespachos = table.Column<int>(type: "integer", nullable: false),
                    VolumenDespachado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VolumenRecibido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InventarioInicial = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InventarioFinal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Diferencias = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresDiariosDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CierresDiariosDetalle_CierresDiarios_CierreDiarioId",
                        column: x => x.CierreDiarioId,
                        principalTable: "CierresDiarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CierresDiariosDetalle_Tanques_TanqueId",
                        column: x => x.TanqueId,
                        principalTable: "Tanques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CierresDiarios_CreadoPorId",
                table: "CierresDiarios",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_CierresDiariosDetalle_CierreDiarioId_TanqueId",
                table: "CierresDiariosDetalle",
                columns: new[] { "CierreDiarioId", "TanqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CierresDiariosDetalle_TanqueId",
                table: "CierresDiariosDetalle",
                column: "TanqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_CierresDiarios_Usuarios_CreadoPorId",
                table: "CierresDiarios",
                column: "CreadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CierresDiarios_Usuarios_CreadoPorId",
                table: "CierresDiarios");

            migrationBuilder.DropTable(
                name: "CierresDiariosDetalle");

            migrationBuilder.DropIndex(
                name: "IX_CierresDiarios_CreadoPorId",
                table: "CierresDiarios");

            migrationBuilder.DropColumn(
                name: "CreadoEn",
                table: "CierresDiarios");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "CierresDiarios");

            migrationBuilder.DropColumn(
                name: "PdfActa",
                table: "CierresDiarios");

            migrationBuilder.DropColumn(
                name: "TotalDespachos",
                table: "CierresDiarios");

            migrationBuilder.AddColumn<string>(
                name: "ActaDigital",
                table: "CierresDiarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReporteUrl",
                table: "CierresDiarios",
                type: "text",
                nullable: true);
        }
    }
}
