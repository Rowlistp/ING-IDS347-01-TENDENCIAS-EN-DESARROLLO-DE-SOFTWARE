using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSecureTicketsQr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(
                name: "ticket_numero_seq");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SolicitudId",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "FirmaDigital",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "QrCodePng",
                table: "Tickets",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "UX_Tickets_Solicitud_Utilizable",
                table: "Tickets",
                column: "SolicitudId",
                unique: true,
                filter: "\"SolicitudId\" IS NOT NULL AND \"Estado\" NOT IN (4, 5, 6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Tickets_Solicitud_Utilizable",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FirmaDigital",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "QrCodePng",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SolicitudId",
                table: "Tickets",
                column: "SolicitudId");

            migrationBuilder.DropSequence(
                name: "ticket_numero_seq");
        }
    }
}
