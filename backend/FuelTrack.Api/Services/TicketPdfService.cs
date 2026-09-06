using FuelTrack.Api.DTOs.Tickets;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Services;

public sealed class TicketPdfService
{
    public byte[] Generate(TicketResponse ticket, byte[] qrCodePng)
        => Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header()
                    .Text("FuelTrack — Ticket digital de combustible")
                    .SemiBold()
                    .FontSize(18)
                    .FontColor(Colors.Blue.Darken2);

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(7);
                    column.Item().Text($"Código: {ticket.Codigo}").Bold().FontSize(15);
                    column.Item().Text($"UUID: {ticket.Id:D}");
                    column.Item().Text($"Empleado: {ticket.EmpleadoNombre} (#{ticket.EmpleadoId})");
                    column.Item().Text($"Vehículo: {ticket.VehiculoPlaca} (#{ticket.VehiculoId})");
                    column.Item().Text($"Departamento: {ticket.DepartamentoNombre}");
                    column.Item().Text($"Combustible: {ticket.TipoCombustibleNombre}");
                    column.Item().Text($"Cantidad autorizada: {ticket.CantidadAutorizada:0.####} galones");
                    column.Item().Text($"Creación UTC: {ticket.FechaCreacion:O}");
                    column.Item().Text($"Vencimiento UTC: {ticket.FechaVencimiento:O}");
                    column.Item().Text($"Estado: {ticket.Estado}");
                    column.Item().PaddingTop(12).Width(260).Image(qrCodePng);
                    column.Item().Text("El QR debe validarse en línea; visualizarlo no consume el ticket.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("FuelTrack · ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
}
