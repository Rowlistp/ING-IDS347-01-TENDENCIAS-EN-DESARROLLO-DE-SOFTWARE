using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase9NotificationsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "Notificaciones" WHERE "Tipo" = 'TICKET_EMITIDO' AND "ReferenciaEvento" IS NOT NULL
                    GROUP BY "Tipo", "ReferenciaEvento", "Canal", btrim("Destinatario") HAVING count(*) > 1) THEN
                    RAISE EXCEPTION 'F9: duplicate historical ticket notifications require explicit operator reconciliation; no records deleted';
                  END IF;
                END $$;
                """);
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadaHastaUtc",
                table: "Notificaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaveIdempotencia",
                table: "Notificaciones",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnviadaEnUtc",
                table: "Notificaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Intentos",
                table: "Notificaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IntentosTotales",
                table: "Notificaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Mensaje",
                table: "Notificaciones",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProveedorMensajeId",
                table: "Notificaciones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximoIntentoUtc",
                table: "Notificaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReservaId",
                table: "Notificaciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoError",
                table: "Notificaciones",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoIntentoUtc",
                table: "Notificaciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Notificaciones" SET "ClaveIdempotencia" = "Tipo" || ':' || "ReferenciaEvento" || ':' || "Canal" || ':' || btrim("Destinatario")
                WHERE "Tipo" = 'TICKET_EMITIDO' AND "ReferenciaEvento" IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "TicketDeliveryLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiraEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevocadoEnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimoAccesoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketDeliveryLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketDeliveryLinks_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_BloqueadaHastaUtc",
                table: "Notificaciones",
                column: "BloqueadaHastaUtc",
                filter: "\"Estado\" = 'PROCESANDO'");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_ClaveIdempotencia",
                table: "Notificaciones",
                column: "ClaveIdempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_ProximoIntentoUtc_Id",
                table: "Notificaciones",
                columns: new[] { "ProximoIntentoUtc", "Id" },
                filter: "\"Estado\" = 'PENDIENTE'");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Tipo_ReferenciaEvento",
                table: "Notificaciones",
                columns: new[] { "Tipo", "ReferenciaEvento" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketDeliveryLinks_TicketId",
                table: "TicketDeliveryLinks",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketDeliveryLinks_TokenHash",
                table: "TicketDeliveryLinks",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketDeliveryLinks");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_BloqueadaHastaUtc",
                table: "Notificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_ClaveIdempotencia",
                table: "Notificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_ProximoIntentoUtc_Id",
                table: "Notificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_Tipo_ReferenciaEvento",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "BloqueadaHastaUtc",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "ClaveIdempotencia",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "EnviadaEnUtc",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "Intentos",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "IntentosTotales",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "Mensaje",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "ProveedorMensajeId",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "ProximoIntentoUtc",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "ReservaId",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "UltimoError",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "UltimoIntentoUtc",
                table: "Notificaciones");
        }
    }
}
