using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5DispatchIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Never invent a tank for historical dispatches: no station/tank relationship exists.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "Despachos") THEN
                    RAISE EXCEPTION 'Phase 5 requires an explicit historical dispatch-to-tank mapping before migration; no data was changed.';
                  END IF;
                END $$;
                """);
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Tickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Inventarios",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<decimal>(
                name: "DisponibilidadRestante",
                table: "Despachos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InventarioRestante",
                table: "Despachos",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TanqueId",
                table: "Despachos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Despachos_TanqueId",
                table: "Despachos",
                column: "TanqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Despachos_Tanques_TanqueId",
                table: "Despachos",
                column: "TanqueId",
                principalTable: "Tanques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despachos_Tanques_TanqueId",
                table: "Despachos");

            migrationBuilder.DropIndex(
                name: "IX_Despachos_TanqueId",
                table: "Despachos");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Inventarios");

            migrationBuilder.DropColumn(
                name: "DisponibilidadRestante",
                table: "Despachos");

            migrationBuilder.DropColumn(
                name: "InventarioRestante",
                table: "Despachos");

            migrationBuilder.DropColumn(
                name: "TanqueId",
                table: "Despachos");
        }
    }
}
