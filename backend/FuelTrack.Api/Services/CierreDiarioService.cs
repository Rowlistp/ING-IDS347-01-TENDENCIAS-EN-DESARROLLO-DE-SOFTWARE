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
        => await db.CierresDiarios
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => c.PdfActa)
            .SingleOrDefaultAsync(ct);

    private static CierreDiarioResponse ToResponse(CierreDiario c,
        IReadOnlyList<CierreDiarioDetalleResponse> detalles, string creadoPorNombre)
        => new(c.Id, c.Fecha, c.TotalDespachos, c.VolumenDespachado,
            c.InventarioFinal, c.Diferencias, c.CreadoPorId, creadoPorNombre,
            c.CreadoEn, c.PdfActa is not null, detalles);

    private static byte[] GenerarPdf(CierreDiario cierre,
        IReadOnlyList<CierreDiarioDetalleResponse> detalles, string creadoPorNombre)
        => Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(s => s.FontSize(10));

            page.Header().Text($"FuelTrack — Acta de Cierre Diario: {cierre.Fecha:yyyy-MM-dd}")
                .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

            page.Content().PaddingVertical(16).Column(col =>
            {
                col.Spacing(6);
                col.Item().Text($"Total despachos: {cierre.TotalDespachos}  |  " +
                    $"Volumen total: {cierre.VolumenDespachado:0.####} gal  |  " +
                    $"Diferencias: {cierre.Diferencias:0.####} gal").Bold();

                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn();
                        c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        c.RelativeColumn(); c.RelativeColumn();
                    });

                    static IContainer HeaderCell(IContainer c) =>
                        c.Background(Colors.Blue.Lighten3).Padding(4);
                    static IContainer DataCell(IContainer c) =>
                        c.BorderBottom(0.5f).Padding(4);

                    table.Header(header =>
                    {
                        foreach (var h in new[] { "Tanque", "Combustible", "Despachos",
                            "Vol.Desp.", "Vol.Recib.", "Inv.Inicial", "Inv.Final", "Dif." })
                            header.Cell().Element(HeaderCell).Text(h).Bold();
                    });

                    foreach (var d in detalles)
                    {
                        table.Cell().Element(DataCell).Text(d.TanqueIdentificacion);
                        table.Cell().Element(DataCell).Text(d.TipoCombustible);
                        table.Cell().Element(DataCell).Text(d.NumeroDespachos.ToString());
                        table.Cell().Element(DataCell).Text($"{d.VolumenDespachado:0.##}");
                        table.Cell().Element(DataCell).Text($"{d.VolumenRecibido:0.##}");
                        table.Cell().Element(DataCell).Text($"{d.InventarioInicial:0.##}");
                        table.Cell().Element(DataCell).Text($"{d.InventarioFinal:0.##}");
                        table.Cell().Element(DataCell)
                            .Text($"{d.Diferencias:0.##}")
                            .FontColor(d.Diferencias == 0 ? Colors.Black : Colors.Red.Medium);
                    }
                });

                col.Item().PaddingTop(20)
                    .Text($"Generado por: {creadoPorNombre} — {cierre.CreadoEn:O} UTC")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            page.Footer().AlignCenter().Text(t => { t.Span("FuelTrack · "); t.CurrentPageNumber(); });
        })).GeneratePdf();
}
