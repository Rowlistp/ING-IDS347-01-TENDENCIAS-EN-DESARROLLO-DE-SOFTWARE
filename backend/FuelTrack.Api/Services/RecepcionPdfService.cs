using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Services;

// Genera el PDF real del comprobante de recepción en el servidor, con el
// mismo patrón de CierreDiarioService/TicketPdfService: se genera una vez,
// se cachea en PdfComprobante y se reutiliza en descargas posteriores.
public sealed class RecepcionPdfService(AppDbContext db)
{
    public async Task<byte[]?> GetPdfAsync(int id, CancellationToken ct)
    {
        var recepcion = await db.RecepcionesCombustible
            .Include(r => r.Proveedor)
            .Include(r => r.Tanque)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (recepcion is null) return null;

        if (recepcion.PdfComprobante is not null && recepcion.PdfComprobante.Length > 0)
            return recepcion.PdfComprobante;

        var pdf = GenerarPdf(recepcion);
        recepcion.PdfComprobante = pdf;
        await db.SaveChangesAsync(ct);
        return pdf;
    }

    private static byte[] GenerarPdf(RecepcionCombustible r)
    {
        var colorTanque = Color.FromHex("#16333A");
        var colorTanque700 = Color.FromHex("#0E2228");
        var colorTanqueClaro = Color.FromHex("#EDF3F4");
        var colorTanqueBorde = Color.FromHex("#D0E3E6");
        var colorAcero = Color.FromHex("#4A5A63");
        var colorAceroClaro = Color.FromHex("#EAEEF0");
        var colorAceroBorde = Color.FromHex("#D3DADE");
        var colorMedidor = Color.FromHex("#E29B2E");
        var colorExito = Color.FromHex("#2E7D5B");
        var colorTinta = Color.FromHex("#12181A");

        var recNumero = $"#REC-{r.Id:D5}";

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(26);
            page.DefaultTextStyle(s => s.FontSize(8.5f).FontColor(colorTinta));

            page.Header().Column(col =>
            {
                col.Item().Border(1).BorderColor(colorAceroBorde).Row(row =>
                {
                    row.ConstantItem(5).Background(colorMedidor);

                    row.RelativeItem().Background(colorTanque).Padding(12).Column(brandCol =>
                    {
                        brandCol.Item().Text("FUELTRACK").ExtraBold().FontSize(18).FontColor(Colors.White).LetterSpacing(0.08f);
                        brandCol.Item().Text("MÓDULO DE SUMINISTRO E INVENTARIO").FontSize(7.5f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.12f);
                    });

                    row.ConstantItem(200).Background(colorTanque).Padding(10).Column(rightCol =>
                    {
                        rightCol.Item().AlignRight().Text("DOCUMENTO OFICIAL").FontSize(7f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.12f);
                        rightCol.Item().AlignRight().Text("COMPROBANTE DE RECEPCIÓN").Bold().FontSize(10f).FontColor(Colors.White);
                        rightCol.Item().PaddingTop(3).AlignRight().Text(recNumero).Bold().FontSize(8.5f).FontColor(colorMedidor);
                    });
                });

                col.Item().Background(colorTanque700).PaddingVertical(10).PaddingHorizontal(14).Column(kpi =>
                {
                    kpi.Item().AlignCenter().Text("VOLUMEN TOTAL DESCARGADO").Bold().FontSize(7f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.1f);
                    kpi.Item().AlignCenter().Text($"{r.VolumenRecibido:0.##} gal").ExtraBold().FontSize(20f).FontColor(Colors.White);
                    kpi.Item().AlignCenter().Text("Carga verificada e ingresada a existencia de inventario").FontSize(7.5f).FontColor(colorExito);
                });
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(8);

                col.Item().Border(1).BorderColor(colorAceroBorde).Column(card =>
                {
                    card.Item().Background(colorTanqueClaro).BorderBottom(1).BorderColor(colorAceroBorde).Padding(5)
                        .Text("DATOS DE LA RECEPCIÓN").Bold().FontSize(7.5f).FontColor(colorTanque).LetterSpacing(0.08f);

                    card.Item().Padding(10).Row(r1 =>
                    {
                        r1.Spacing(14);
                        r1.RelativeItem().Element(c => Field(c, "Proveedor Suplidor", r.Proveedor.Nombre));
                        r1.RelativeItem().Element(c => Field(c, "RNC Suplidor", string.IsNullOrWhiteSpace(r.Proveedor.Rnc) ? "—" : r.Proveedor.Rnc));
                    });
                    card.Item().PaddingHorizontal(10).PaddingBottom(10).Row(r2 =>
                    {
                        r2.Spacing(14);
                        r2.RelativeItem().Element(c => Field(c, "Factura / Conduce Suplidor", r.NumeroFactura));
                        r2.RelativeItem().Element(c => Field(c, "Tanque Receptor de Destino", r.Tanque.Identificacion));
                    });
                    card.Item().BorderTop(1).BorderColor(colorAceroBorde).Padding(10)
                        .Text($"Fecha y Hora de Descarga: {r.Fecha:dd/MM/yyyy HH:mm} UTC").Bold().FontSize(9f);
                });

                col.Item().PaddingTop(24).Row(sigs =>
                {
                    sigs.Spacing(24);

                    static void SignatureBox(IContainer c, string title, string sub)
                    {
                        c.Column(sc =>
                        {
                            sc.Item().LineHorizontal(1).LineColor(Color.FromHex("#16333A"));
                            sc.Item().PaddingTop(3).AlignCenter().Text(title).Bold().FontSize(8f);
                            sc.Item().AlignCenter().Text(sub).FontSize(7f).FontColor(Color.FromHex("#4A5A63"));
                        });
                    }

                    sigs.RelativeItem().Element(c => SignatureBox(c, "Transportista / Chofer Suplidor", "Cédula y Ficha del Camión Cisterna"));
                    sigs.RelativeItem().Element(c => SignatureBox(c, "Operador de Estación / Receptor", "Firma y Sello de Recepción Conforme"));
                });
            });

            page.Footer().BorderTop(1).BorderColor(colorAceroBorde).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("FuelTrack v2.1 · Registro inmutable de abastecimiento de combustible · ");
                    t.Span($"Emitido: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontColor(colorAcero);
                });

                row.AutoItem().Background(colorAceroClaro).Border(0.5f).BorderColor(colorAceroBorde).PaddingHorizontal(5).PaddingVertical(1)
                    .Text("CONFIDENCIAL").Bold().FontSize(6.5f).FontColor(colorAcero);

                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span($" · Ref: {recNumero}");
                });
            });
        })).GeneratePdf();

        static void Field(IContainer c, string label, string value)
        {
            c.Column(col =>
            {
                col.Item().Text(label).Bold().FontSize(7f).FontColor(Color.FromHex("#4A5A63")).LetterSpacing(0.06f);
                col.Item().Text(value).FontSize(9f).Bold();
            });
        }
    }
}
