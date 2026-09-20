using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Cierres;
using FuelTrack.Api.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Services;

public sealed class CierreDiarioService(AppDbContext db, AuditService audit)
{
    public async Task<CierreDiarioResponse> GenerarAsync(DateOnly fecha, int actorId, string? ip, CancellationToken ct)
    {
        if (fecha > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new TicketDomainException(400, "FECHA_FUTURA", "No se puede cerrar un día futuro.");

        var tanqueIds = await db.Despachos
            .Where(d => d.Fecha == fecha)
            .Select(d => d.TanqueId)
            .Distinct()
            .ToListAsync(ct);

        if (tanqueIds.Count == 0)
            throw new TicketDomainException(400, "SIN_DESPACHOS", "No hay despachos registrados para la fecha indicada.");

        if (await db.CierresDiarios.AnyAsync(c => c.Fecha == fecha, ct))
            throw new TicketDomainException(409, "CIERRE_YA_EXISTE", "Ya existe un cierre para la fecha indicada.");

        var dayStart = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = fecha.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var actor = await db.Usuarios.SingleAsync(u => u.Id == actorId, ct);
            var cierre = new CierreDiario
            {
                Fecha = fecha,
                CreadoPorId = actorId,
                CreadoEn = DateTime.UtcNow
            };
            db.CierresDiarios.Add(cierre);
            await db.SaveChangesAsync(ct);

            var detalles = new List<CierreDiarioDetalle>();
            var detalleResponses = new List<CierreDiarioDetalleResponse>();

            foreach (var tanqueId in tanqueIds)
            {
                var tanque = await db.Tanques
                    .Include(t => t.TipoCombustible)
                    .SingleAsync(t => t.Id == tanqueId, ct);
                var inventario = await db.Inventarios
                    .SingleAsync(i => i.TanqueId == tanqueId, ct);

                var movsDespues = await db.MovimientosInventario
                    .Where(m => m.TanqueId == tanqueId && m.FechaHora > dayEnd)
                    .SumAsync(m => (decimal?)m.Volumen, ct) ?? 0m;

                var movsDia = await db.MovimientosInventario
                    .Where(m => m.TanqueId == tanqueId && m.FechaHora >= dayStart && m.FechaHora <= dayEnd)
                    .SumAsync(m => (decimal?)m.Volumen, ct) ?? 0m;

                var inventarioFinalReal = inventario.ExistenciaActual - movsDespues;
                var inventarioInicial = inventarioFinalReal - movsDia;

                var despachosDia = await db.Despachos
                    .Where(d => d.TanqueId == tanqueId && d.Fecha == fecha)
                    .ToListAsync(ct);
                var volumenDespachado = despachosDia.Sum(d => d.GalonesServidos);
                var numeroDespachos = despachosDia.Count;

                var volumenRecibido = await db.RecepcionesCombustible
                    .Where(r => r.TanqueId == tanqueId && r.Fecha >= dayStart && r.Fecha <= dayEnd)
                    .SumAsync(r => (decimal?)r.VolumenRecibido, ct) ?? 0m;

                var inventarioFinalTeorico = inventarioInicial + volumenRecibido - volumenDespachado;
                var diferencias = inventarioFinalReal - inventarioFinalTeorico;

                var detalle = new CierreDiarioDetalle
                {
                    CierreDiarioId = cierre.Id,
                    TanqueId = tanqueId,
                    NumeroDespachos = numeroDespachos,
                    VolumenDespachado = volumenDespachado,
                    VolumenRecibido = volumenRecibido,
                    InventarioInicial = inventarioInicial,
                    InventarioFinal = inventarioFinalReal,
                    Diferencias = diferencias
                };
                detalles.Add(detalle);
                detalleResponses.Add(new CierreDiarioDetalleResponse(
                    tanqueId, tanque.Identificacion, tanque.TipoCombustible.Nombre,
                    numeroDespachos, volumenDespachado, volumenRecibido,
                    inventarioInicial, inventarioFinalReal, diferencias));
            }

            db.CierresDiariosDetalle.AddRange(detalles);

            cierre.VolumenDespachado = detalles.Sum(d => d.VolumenDespachado);
            cierre.InventarioFinal = detalles.Sum(d => d.InventarioFinal);
            cierre.Diferencias = detalles.Sum(d => d.Diferencias);
            cierre.TotalDespachos = detalles.Sum(d => d.NumeroDespachos);
            cierre.PdfActa = GenerarPdf(cierre, detalleResponses, actor.NombreUsuario);

            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("CIERRE_GENERADO", "CierreDiario", cierre.Id.ToString(), actorId, ip,
                new { cierre.Fecha, cierre.TotalDespachos, cierre.VolumenDespachado }, ct);
            await transaction.CommitAsync(ct);

            return ToResponse(cierre, detalleResponses, actor.NombreUsuario);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<CierreDiarioResponse>> GetAllAsync(int pagina, int tamanoPagina, CancellationToken ct)
        => (await db.CierresDiarios
            .AsNoTracking()
            .Include(c => c.CreadoPor)
            .Include(c => c.Detalles).ThenInclude(d => d.Tanque).ThenInclude(t => t.TipoCombustible)
            .OrderByDescending(c => c.Fecha)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .ToListAsync(ct))
            .Select(c => ToResponse(c, c.Detalles.Select(d => new CierreDiarioDetalleResponse(
                d.TanqueId, d.Tanque.Identificacion, d.Tanque.TipoCombustible.Nombre,
                d.NumeroDespachos, d.VolumenDespachado, d.VolumenRecibido,
                d.InventarioInicial, d.InventarioFinal, d.Diferencias)).ToList(),
                c.CreadoPor.NombreUsuario))
            .ToList();

    public async Task<CierreDiarioResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var c = await db.CierresDiarios
            .AsNoTracking()
            .Include(c => c.CreadoPor)
            .Include(c => c.Detalles).ThenInclude(d => d.Tanque).ThenInclude(t => t.TipoCombustible)
            .SingleOrDefaultAsync(c => c.Id == id, ct);
        if (c is null) return null;
        var detalleResponses = c.Detalles.Select(d => new CierreDiarioDetalleResponse(
            d.TanqueId, d.Tanque.Identificacion, d.Tanque.TipoCombustible.Nombre,
            d.NumeroDespachos, d.VolumenDespachado, d.VolumenRecibido,
            d.InventarioInicial, d.InventarioFinal, d.Diferencias)).ToList();
        return ToResponse(c, detalleResponses, c.CreadoPor.NombreUsuario);
    }

    public async Task<byte[]?> GetPdfAsync(int id, CancellationToken ct)
    {
        var cierre = await db.CierresDiarios
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Tanque)
                    .ThenInclude(t => t.TipoCombustible)
            .Include(c => c.CreadoPor)
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        if (cierre is null) return null;

        if (cierre.PdfActa is not null && cierre.PdfActa.Length > 0)
            return cierre.PdfActa;

        var detalleResponses = cierre.Detalles.Select(d => new CierreDiarioDetalleResponse(
            d.TanqueId, d.Tanque.Identificacion, d.Tanque.TipoCombustible.Nombre,
            d.NumeroDespachos, d.VolumenDespachado, d.VolumenRecibido,
            d.InventarioInicial, d.InventarioFinal, d.Diferencias)).ToList();

        var pdf = GenerarPdf(cierre, detalleResponses, cierre.CreadoPor.NombreUsuario);
        cierre.PdfActa = pdf;
        await db.SaveChangesAsync(ct);
        return pdf;
    }

    private static CierreDiarioResponse ToResponse(CierreDiario c,
        IReadOnlyList<CierreDiarioDetalleResponse> detalles, string creadoPorNombre)
        => new(c.Id, c.Fecha, c.TotalDespachos, c.VolumenDespachado,
            c.InventarioFinal, c.Diferencias, c.CreadoPorId, creadoPorNombre,
            c.CreadoEn, true, detalles);

    private static byte[] GenerarPdf(CierreDiario cierre,
        IReadOnlyList<CierreDiarioDetalleResponse> detalles, string creadoPorNombre)
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
        var colorExitoClaro = Color.FromHex("#EDF8F3");
        var colorPeligro = Color.FromHex("#C1432B");
        var colorPeligroClaro = Color.FromHex("#FDF1EE");
        var colorTinta = Color.FromHex("#12181A");

        var totalRecibido = detalles.Sum(d => d.VolumenRecibido);

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
                        brandCol.Item().Text("SISTEMA DE GESTIÓN DE COMBUSTIBLE").FontSize(7.5f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.12f);
                    });

                    row.ConstantItem(180).Background(colorTanque).Padding(10).Column(rightCol =>
                    {
                        rightCol.Item().AlignRight().Text("DOCUMENTO OFICIAL").FontSize(7f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.12f);
                        rightCol.Item().AlignRight().Text("ACTA DE CIERRE DIARIO").Bold().FontSize(10f).FontColor(Colors.White);
                        rightCol.Item().PaddingTop(3).AlignRight().Text($"FECHA: {cierre.Fecha:dd/MM/yyyy}").Bold().FontSize(8.5f).FontColor(colorMedidor);
                    });
                });

                // Metrics band
                col.Item().Background(colorTanque700).PaddingVertical(8).PaddingHorizontal(14).Row(metrics =>
                {
                    static void MetricItem(IContainer c, string label, string val, Color? valColor = null)
                    {
                        c.Column(mc =>
                        {
                            mc.Item().AlignCenter().Text(label).Bold().FontSize(6.5f).FontColor(Color.FromHex("#93C5FD")).LetterSpacing(0.08f);
                            mc.Item().AlignCenter().Text(val).ExtraBold().FontSize(14f).FontColor(valColor ?? Colors.White);
                        });
                    }

                    metrics.RelativeItem().Element(c => MetricItem(c, "TOTAL DESPACHOS", cierre.TotalDespachos.ToString()));
                    metrics.ConstantItem(1).Background(Color.FromHex("#2C4850"));
                    metrics.RelativeItem().Element(c => MetricItem(c, "VOLUMEN DESPACHADO", $"{cierre.VolumenDespachado:0.##} gal"));
                    metrics.ConstantItem(1).Background(Color.FromHex("#2C4850"));
                    metrics.RelativeItem().Element(c => MetricItem(c, "VOLUMEN RECIBIDO", $"{totalRecibido:0.##} gal", colorExito));
                    metrics.ConstantItem(1).Background(Color.FromHex("#2C4850"));
                    metrics.RelativeItem().Element(c => MetricItem(c, "DIFERENCIAS TOTALES", $"{cierre.Diferencias:0.##} gal", cierre.Diferencias == 0 ? Colors.White : colorMedidor));
                });
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(8);

                // Período y Responsable cards
                col.Item().Row(r =>
                {
                    r.Spacing(8);

                    r.RelativeItem().Border(1).BorderColor(colorAceroBorde).Column(card =>
                    {
                        card.Item().Background(colorTanqueClaro).BorderBottom(1).BorderColor(colorAceroBorde).Padding(5)
                            .Text("PERÍODO DEL CIERRE").Bold().FontSize(7.5f).FontColor(colorTanque).LetterSpacing(0.08f);
                        card.Item().Padding(8).Column(inner =>
                        {
                            inner.Spacing(2);
                            inner.Item().Text($"Fecha Operacional: {cierre.Fecha:dd/MM/yyyy}").Bold().FontSize(9f);
                            inner.Item().Text($"Hora de Cierre: {cierre.CreadoEn:dd/MM/yyyy HH:mm:ss} UTC").FontSize(8f).FontColor(colorAcero);
                            inner.Item().Text("Turno: Completo (00:00 – 23:59)").FontSize(8f).FontColor(colorAcero);
                        });
                    });

                    r.RelativeItem().Border(1).BorderColor(colorAceroBorde).Column(card =>
                    {
                        card.Item().Background(colorTanqueClaro).BorderBottom(1).BorderColor(colorAceroBorde).Padding(5)
                            .Text("RESPONSABLE DEL REGISTRO").Bold().FontSize(7.5f).FontColor(colorTanque).LetterSpacing(0.08f);
                        card.Item().Padding(8).Column(inner =>
                        {
                            inner.Spacing(2);
                            inner.Item().Text($"Generado por: {creadoPorNombre}").Bold().FontSize(9f);
                            inner.Item().Text($"ID Usuario: #{cierre.CreadoPorId}").FontSize(8f).FontColor(colorAcero);
                            inner.Item().Text("Responsable del cierre registrado").FontSize(8f).FontColor(colorAcero);
                        });
                    });
                });

                // Table title
                col.Item().Row(r =>
                {
                    r.AutoItem().Text("ESTADO DE TANQUES E INVENTARIOS AL CIERRE").Bold().FontSize(8f).FontColor(colorTanque).LetterSpacing(0.08f);
                });

                // Detailed Table
                col.Item().Border(1).BorderColor(colorAceroBorde).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.8f);
                        c.RelativeColumn(2.2f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.2f);
                    });

                    table.Header(h =>
                    {
                        static IContainer HeaderCell(IContainer c, Color bg) =>
                            c.Background(bg).PaddingVertical(5).PaddingHorizontal(4);

                        h.Cell().Element(c => HeaderCell(c, colorTanque)).Text("TANQUE").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).Text("COMBUSTIBLE").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).AlignRight().Text("DESP.").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).AlignRight().Text("VOL.DESP.").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).AlignRight().Text("VOL.REC.").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).AlignRight().Text("INV.INIC.").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).AlignRight().Text("INV.FINAL").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Element(c => HeaderCell(c, colorTanque)).AlignRight().Text("DIF.").Bold().FontSize(7.5f).FontColor(Colors.White);
                    });

                    var idx = 0;
                    foreach (var d in detalles)
                    {
                        var rowBg = (idx++ % 2 == 0) ? Colors.White : colorTanqueClaro;
                        IContainer DataCell(IContainer c) => c.Background(rowBg).BorderBottom(0.5f).BorderColor(colorAceroClaro).PaddingVertical(4).PaddingHorizontal(4);

                        table.Cell().Element(DataCell).Text(d.TanqueIdentificacion).Bold().FontSize(8f);
                        table.Cell().Element(DataCell).Text(d.TipoCombustible).FontSize(8f);
                        table.Cell().Element(DataCell).AlignRight().Text(d.NumeroDespachos.ToString()).FontSize(8f);
                        table.Cell().Element(DataCell).AlignRight().Text($"{d.VolumenDespachado:0.##}").Bold().FontColor(colorPeligro).FontSize(8f);
                        table.Cell().Element(DataCell).AlignRight().Text($"{d.VolumenRecibido:0.##}").Bold().FontColor(colorExito).FontSize(8f);
                        table.Cell().Element(DataCell).AlignRight().Text($"{d.InventarioInicial:0.##}").FontSize(8f);
                        table.Cell().Element(DataCell).AlignRight().Text($"{d.InventarioFinal:0.##}").Bold().FontSize(8f);
                        table.Cell().Element(DataCell).AlignRight()
                            .Text($"{d.Diferencias:0.##}")
                            .Bold().FontSize(8f)
                            .FontColor(d.Diferencias == 0 ? colorAcero : colorMedidor);
                    }
                });

                // Totals Bar
                col.Item().Background(colorTanqueClaro).Border(1).BorderColor(colorTanqueBorde).Padding(7).Row(tb =>
                {
                    tb.AutoItem().Text("TOTALES DEL DÍA: ").Bold().FontSize(8f).FontColor(colorTanque);
                    tb.RelativeItem().PaddingLeft(8).Row(inner =>
                    {
                        inner.Spacing(14);
                        inner.AutoItem().Text($"Despachos: {cierre.TotalDespachos}").FontSize(8f);
                        inner.AutoItem().Text($"Vol. Despachado: {cierre.VolumenDespachado:0.##} gal").Bold().FontColor(colorPeligro).FontSize(8f);
                        inner.AutoItem().Text($"Vol. Recibido: {totalRecibido:0.##} gal").Bold().FontColor(colorExito).FontSize(8f);
                        inner.AutoItem().Text($"Inv. Final Acumulado: {cierre.InventarioFinal:0.##} gal").Bold().FontSize(8f);
                    });
                });

                // Signatures
                col.Item().PaddingTop(18).Row(sigs =>
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

                    sigs.RelativeItem().Element(c => SignatureBox(c, creadoPorNombre, "Responsable / Elaboró"));
                    sigs.RelativeItem().Element(c => SignatureBox(c, "Gerencia de Operaciones", "Revisión y Aprobación"));
                    sigs.RelativeItem().Element(c => SignatureBox(c, "Auditoría Interna", "V.B. y Conforme"));
                });
            });

            page.Footer().BorderTop(1).BorderColor(colorAceroBorde).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("FuelTrack v2.1 · Acta oficial de cierre operacional · ");
                    t.Span($"Emitido: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontColor(colorAcero);
                });

                row.AutoItem().Background(colorAceroClaro).Border(0.5f).BorderColor(colorAceroBorde).PaddingHorizontal(5).PaddingVertical(1)
                    .Text("CONFIDENCIAL").Bold().FontSize(6.5f).FontColor(colorAcero);

                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span($" · Ref: CD-{cierre.Fecha:yyyy-MM-dd}");
                });
            });
        })).GeneratePdf();
    }
}
