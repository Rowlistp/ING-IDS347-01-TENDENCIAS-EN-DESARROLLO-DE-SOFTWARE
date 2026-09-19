using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Models.Enums;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Services;

public sealed class TicketPdfService
{
    private static readonly Color ColorTanque = Color.FromHex("#16333A");
    private static readonly Color ColorTanqueClaro = Color.FromHex("#EDF3F4");
    private static readonly Color ColorTanqueBorde = Color.FromHex("#D0E3E6");
    private static readonly Color ColorAcero = Color.FromHex("#4A5A63");
    private static readonly Color ColorAceroClaro = Color.FromHex("#EAEEF0");
    private static readonly Color ColorAceroBorde = Color.FromHex("#D3DADE");
    private static readonly Color ColorMedidor = Color.FromHex("#E29B2E");
    private static readonly Color ColorMedidorClaro = Color.FromHex("#FDF3DC");
    private static readonly Color ColorMedidorTexto = Color.FromHex("#A06808");
    private static readonly Color ColorExito = Color.FromHex("#2E7D5B");
    private static readonly Color ColorExitoClaro = Color.FromHex("#EDF8F3");
    private static readonly Color ColorExitoBorde = Color.FromHex("#A7D9C4");
    private static readonly Color ColorPeligro = Color.FromHex("#C1432B");
    private static readonly Color ColorPeligroClaro = Color.FromHex("#FDF1EE");
    private static readonly Color ColorInfo = Color.FromHex("#2F6F8F");
    private static readonly Color ColorFondo = Color.FromHex("#F7F8F6");
    private static readonly Color ColorTinta = Color.FromHex("#12181A");

    public byte[] Generate(TicketResponse ticket, byte[]? qrCodePng)
    {
        var validQr = EnsureValidQrPng(ticket, qrCodePng);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(26);
                page.DefaultTextStyle(style => style.FontSize(9).FontColor(ColorTinta));

                page.Header().Element(header => ComposeHeader(header, ticket));
                page.Content().PaddingTop(10).Element(content => ComposeContent(content, ticket, validQr));
                page.Footer().Element(footer => ComposeFooter(footer, ticket));
            });
        }).GeneratePdf();
    }

    private static byte[] EnsureValidQrPng(TicketResponse ticket, byte[]? qrCodePng)
    {
        if (qrCodePng is not null && qrCodePng.Length > 100)
            return qrCodePng;

        using var qrData = QRCodeGenerator.GenerateQrCode(
            $"FTQR1.{ticket.Codigo}.{ticket.Id:D}.{ticket.CantidadAutorizada:0.##}.{ticket.FechaVencimiento:yyyyMMddHHmmss}",
            QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8);
    }

    private static void ComposeHeader(IContainer container, TicketResponse ticket)
    {
        container.Column(col =>
        {
            col.Item().Border(1).BorderColor(ColorAceroBorde).Row(row =>
            {
                row.ConstantItem(5).Background(ColorMedidor);

                row.RelativeItem().Background(ColorTanque).Padding(12).Row(r =>
                {
                    r.RelativeItem().Column(brandCol =>
                    {
                        brandCol.Item().Text("FUELTRACK").ExtraBold().FontSize(18).FontColor(Colors.White).LetterSpacing(0.08f);
                        brandCol.Item().Text("SISTEMA DE GESTIÓN DE COMBUSTIBLE").FontSize(7.5f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.12f);
                    });
                });

                row.ConstantItem(180).Background(ColorTanque).Padding(10).Column(rightCol =>
                {
                    rightCol.Item().AlignRight().Text("DOCUMENTO OFICIAL").FontSize(7f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.12f);
                    rightCol.Item().AlignRight().Text("AUTORIZACIÓN DE COMBUSTIBLE").Bold().FontSize(9.5f).FontColor(Colors.White);

                    rightCol.Item().PaddingTop(4).AlignRight().Element(el =>
                    {
                        var (bg, fg, border) = ticket.Estado switch
                        {
                            EstadoTicket.Pendiente or EstadoTicket.Creado => (ColorMedidorClaro, ColorMedidorTexto, ColorMedidor),
                            EstadoTicket.Consumido => (ColorExitoClaro, ColorExito, ColorExitoBorde),
                            EstadoTicket.Anulado or EstadoTicket.Vencido => (ColorPeligroClaro, ColorPeligro, ColorPeligro),
                            _ => (ColorAceroClaro, ColorAcero, ColorAceroBorde)
                        };

                        el.Background(bg).Border(1).BorderColor(border).PaddingHorizontal(6).PaddingVertical(2)
                          .Text($"● {ticket.Estado.ToString().ToUpperInvariant()}")
                          .Bold().FontSize(7.5f).FontColor(fg);
                    });
                });
            });

            col.Item().Background(ColorTanqueClaro).BorderHorizontal(1).BorderColor(ColorTanqueBorde)
                .PaddingHorizontal(14).PaddingVertical(6).Row(sub =>
                {
                    sub.RelativeItem().Row(r =>
                    {
                        r.AutoItem().Text("Ticket N.º ").FontSize(9.5f).FontColor(ColorAcero);
                        r.AutoItem().Text(ticket.Codigo).Bold().FontSize(12f).FontColor(ColorTanque);
                    });

                    sub.RelativeItem().AlignRight().Row(r =>
                    {
                        r.AutoItem().Text($"Emitido: {ticket.FechaCreacion:dd/MM/yyyy HH:mm}").FontSize(8.5f).FontColor(ColorAcero);
                        r.AutoItem().PaddingHorizontal(6).Text("·").FontColor(ColorAceroBorde);
                        r.AutoItem().Text($"Válido hasta: {ticket.FechaVencimiento:dd/MM/yyyy}").Bold().FontSize(8.5f).FontColor(ColorPeligro);
                    });
                });
        });
    }

    private static void ComposeContent(IContainer container, TicketResponse ticket, byte[] qrPng)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Row(row =>
            {
                row.Spacing(8);

                row.RelativeItem().Border(1).BorderColor(ColorAceroBorde).Column(c =>
                {
                    c.Item().Background(ColorTanqueClaro).BorderBottom(1).BorderColor(ColorAceroBorde).Padding(5)
                        .Text("SOLICITANTE").Bold().FontSize(7.5f).FontColor(ColorTanque).LetterSpacing(0.1f);
                    c.Item().Padding(8).Column(inner =>
                    {
                        inner.Spacing(2);
                        inner.Item().Text(ticket.EmpleadoNombre).Bold().FontSize(10.5f);
                        inner.Item().Text($"Departamento: {ticket.DepartamentoNombre}").FontSize(8.5f).FontColor(ColorAcero);
                        inner.Item().LineHorizontal(0.5f).LineColor(ColorAceroClaro);
                        inner.Item().Text($"ID Empleado: #{ticket.EmpleadoId}").FontSize(8f).FontColor(ColorAcero);
                        if (ticket.SolicitudId.HasValue)
                            inner.Item().Text($"Solicitud Ref.: #{ticket.SolicitudId}").FontSize(8f).FontColor(ColorAcero);
                    });
                });

                row.RelativeItem().Border(1).BorderColor(ColorAceroBorde).Column(c =>
                {
                    c.Item().Background(ColorTanqueClaro).BorderBottom(1).BorderColor(ColorAceroBorde).Padding(5)
                        .Text("VEHÍCULO ASIGNADO").Bold().FontSize(7.5f).FontColor(ColorTanque).LetterSpacing(0.1f);
                    c.Item().Padding(8).Column(inner =>
                    {
                        inner.Spacing(2);
                        inner.Item().Text($"Placa: {ticket.VehiculoPlaca}").Bold().FontSize(10.5f);
                        inner.Item().Text($"Combustible: {ticket.TipoCombustibleNombre}").FontSize(8.5f).FontColor(ColorAcero);
                        inner.Item().LineHorizontal(0.5f).LineColor(ColorAceroClaro);
                        inner.Item().Text($"ID Vehículo: #{ticket.VehiculoId}").FontSize(8f).FontColor(ColorAcero);
                        inner.Item().Text($"Prefijo: {ticket.Prefijo}").FontSize(8f).FontColor(ColorAcero);
                    });
                });
            });

            col.Item().Border(1.5f).BorderColor(ColorTanque).Column(c =>
            {
                c.Item().Background(ColorTanque).Padding(7).Row(r =>
                {
                    r.RelativeItem().Text("AUTORIZACIÓN DE DESPACHO").Bold().FontSize(8.5f).FontColor(Colors.White).LetterSpacing(0.1f);
                    r.AutoItem().Text($"Ref. Secuencial: {ticket.NumeroSecuencial:D6}").FontSize(8f).FontColor(ColorMedidor);
                });

                c.Item().Background(ColorTanqueClaro).Padding(12).Row(r =>
                {
                    r.ConstantItem(150).Column(gallonsCol =>
                    {
                        gallonsCol.Item().Text("CANTIDAD AUTORIZADA").Bold().FontSize(7.5f).FontColor(ColorAcero).LetterSpacing(0.08f);
                        gallonsCol.Item().Text($"{ticket.CantidadAutorizada:0.##}").ExtraBold().FontSize(34f).FontColor(ColorTanque);
                        gallonsCol.Item().Text("GALONES").Bold().FontSize(10.5f).FontColor(ColorInfo);
                    });

                    r.ConstantItem(1).Background(ColorTanqueBorde);

                    r.RelativeItem().PaddingLeft(14).Column(detailsCol =>
                    {
                        detailsCol.Spacing(3);
                        detailsCol.Item().Text(t =>
                        {
                            t.Span("Tipo de Combustible: ").FontColor(ColorAcero);
                            t.Span(ticket.TipoCombustibleNombre).Bold();
                        });
                        detailsCol.Item().Text(t =>
                        {
                            t.Span("Válido hasta: ").FontColor(ColorAcero);
                            t.Span($"{ticket.FechaVencimiento:dd/MM/yyyy HH:mm}").Bold().FontColor(ColorPeligro);
                        });
                        detailsCol.Item().Text(t =>
                        {
                            t.Span("Estado: ").FontColor(ColorAcero);
                            t.Span(ticket.Estado.ToString()).Bold();
                        });
                        if (!string.IsNullOrWhiteSpace(ticket.MotivoAnulacion))
                        {
                            detailsCol.Item().Text(t =>
                            {
                                t.Span("Motivo Anulación: ").FontColor(ColorPeligro);
                                t.Span(ticket.MotivoAnulacion).Bold().FontColor(ColorPeligro);
                            });
                        }
                    });
                });
            });

            col.Item().Row(r =>
            {
                r.Spacing(10);

                r.ConstantItem(100).Border(1).BorderColor(ColorAceroBorde).Padding(6).Column(qrCol =>
                {
                    qrCol.Item().Image(qrPng);
                    qrCol.Item().PaddingTop(3).AlignCenter().Text("Escanear para validar")
                        .FontSize(6.5f).FontColor(ColorAcero);
                });

                r.RelativeItem().Border(1).BorderColor(ColorAceroBorde).Background(ColorFondo).Padding(8).Column(secCol =>
                {
                    secCol.Item().Row(titleRow =>
                    {
                        titleRow.AutoItem().Text("SELLO CRIPTOGRÁFICO Y CONTROL").Bold().FontSize(7.5f).FontColor(ColorTanque).LetterSpacing(0.08f);
                    });
                    secCol.Item().PaddingTop(4).Column(kvs =>
                    {
                        kvs.Spacing(2);
                        kvs.Item().Text(t => { t.Span("UUID: ").FontColor(ColorAcero); t.Span(ticket.Id.ToString("D")).FontSize(7.5f); });
                        kvs.Item().Text(t => { t.Span("Seguridad: ").FontColor(ColorAcero); t.Span("Firma digital ECDSA P-256 / Hash SHA-256").FontSize(7.5f); });
                        kvs.Item().Text(t => { t.Span("Protocolo: ").FontColor(ColorAcero); t.Span("FTQR1 · Validación en línea obligatoria").FontSize(7.5f); });
                    });
                    secCol.Item().PaddingTop(6).Background(ColorExitoClaro).Border(1).BorderColor(ColorExitoBorde).Padding(4).Row(v =>
                    {
                        v.AutoItem().Text("✔ DOCUMENTO AUTÉNTICO — Emitido por sistema oficial FuelTrack")
                            .Bold().FontSize(7f).FontColor(ColorExito);
                    });
                });
            });

            col.Item().Background(ColorMedidorClaro).Border(1).BorderColor(ColorMedidor).Padding(7).Column(obs =>
            {
                obs.Item().Text("OBSERVACIONES Y POLÍTICA DE DESPACHO").Bold().FontSize(7f).FontColor(ColorMedidorTexto).LetterSpacing(0.08f);
                obs.Item().PaddingTop(1).Text("Despachar únicamente en presencia del empleado y vehículo autorizados. No fraccionar el volumen. El QR debe validarse en línea; visualizar este documento no consume el saldo del ticket.")
                    .FontSize(7.5f).FontColor(ColorTinta);
            });

            col.Item().PaddingTop(14).Row(sigs =>
            {
                sigs.Spacing(20);

                static void SignatureBox(IContainer c, string title, string sub)
                {
                    c.Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(ColorTanque);
                        col.Item().PaddingTop(3).AlignCenter().Text(title).Bold().FontSize(8f);
                        col.Item().AlignCenter().Text(sub).FontSize(7f).FontColor(ColorAcero);
                    });
                }

                sigs.RelativeItem().Element(c => SignatureBox(c, "Firma del Despachador", "Estación de Combustible"));
                sigs.RelativeItem().Element(c => SignatureBox(c, "Firma del Conductor / Portador", "Empleado Solicitante"));
                sigs.RelativeItem().Element(c => SignatureBox(c, "Visto Bueno Supervisor", "Control y Auditoría"));
            });
        });
    }

    private static void ComposeFooter(IContainer container, TicketResponse ticket)
    {
        container.BorderTop(1).BorderColor(ColorAceroBorde).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("FuelTrack v2.1 · Documento oficial de control interno · ");
                t.Span($"Generado el {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC").FontColor(ColorAcero);
            });

            row.AutoItem().Background(ColorAceroClaro).Border(0.5f).BorderColor(ColorAceroBorde).PaddingHorizontal(5).PaddingVertical(1)
                .Text("USO INTERNO").Bold().FontSize(6.5f).FontColor(ColorAcero);

            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" · Ref: ");
                t.Span(ticket.Codigo).Bold();
            });
        });
    }
}
