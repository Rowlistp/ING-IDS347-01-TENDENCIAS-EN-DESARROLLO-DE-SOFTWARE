using System.Text;
using ClosedXML.Excel;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Reportes;
using FuelTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FuelTrack.Api.Services;

public sealed class ReporteService(AppDbContext db)
{
    private static readonly HashSet<string> TiposValidos =
        ["solicitudes", "despachos", "inventario", "cierres"];
    private static readonly HashSet<string> FormatosValidos =
        ["csv", "excel", "pdf"];

    public async Task<ReportePageResponse> GetAsync(ReporteQuery q, CancellationToken ct)
    {
        ValidarTipo(q.Tipo);
        return q.Tipo switch
        {
            "solicitudes" => await GetSolicitudesAsync(q, ct),
            "despachos"   => await GetDespachosAsync(q, ct),
            "inventario"  => await GetInventarioAsync(q, ct),
            "cierres"     => await GetCierresAsync(q, ct),
            _             => throw new TicketDomainException(400, "TIPO_REPORTE_INVALIDO", "Tipo no válido.")
        };
    }

    public async Task<byte[]> ExportarAsync(ReporteQuery q, string formato, CancellationToken ct)
    {
        ValidarTipo(q.Tipo);
        if (!FormatosValidos.Contains(formato))
            throw new TicketDomainException(400, "FORMATO_INVALIDO",
                "Formato no válido. Use: csv, excel, pdf.");

        var fullQuery = q with { Pagina = 1, TamanoPagina = int.MaxValue };
        var page = await GetAsync(fullQuery, ct);
        return formato switch
        {
            "csv"   => ExportarCsv(page),
            "excel" => ExportarExcel(page),
            "pdf"   => ExportarPdf(page),
            _       => throw new TicketDomainException(400, "FORMATO_INVALIDO", "Formato no válido.")
        };
    }

    // ── Queries ──────────────────────────────────────────────────────────

    private async Task<ReportePageResponse> GetSolicitudesAsync(ReporteQuery q, CancellationToken ct)
    {
        var query = db.SolicitudesCombustible.AsNoTracking()
            .Include(s => s.Empleado).Include(s => s.Vehiculo)
            .Include(s => s.Departamento).Include(s => s.TipoCombustible)
            .AsQueryable();

        if (q.FechaDesde.HasValue)
            query = query.Where(s => DateOnly.FromDateTime(s.FechaSolicitud) >= q.FechaDesde.Value);
        if (q.FechaHasta.HasValue)
            query = query.Where(s => DateOnly.FromDateTime(s.FechaSolicitud) <= q.FechaHasta.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(s => s.FechaSolicitud)
            .Skip((q.Pagina - 1) * q.TamanoPagina).Take(q.TamanoPagina)
            .ToListAsync(ct);

        return new ReportePageResponse("solicitudes", total, q.Pagina, q.TamanoPagina,
            items.Select(s => (object)new SolicitudReporteDto(
                s.Id, s.FechaSolicitud, s.Empleado.NombreCompleto, s.Vehiculo.Placa,
                s.Departamento.Nombre, s.TipoCombustible.Nombre,
                s.CantidadSolicitada, s.CantidadAutorizada, s.Estado)).ToList());
    }

    private async Task<ReportePageResponse> GetDespachosAsync(ReporteQuery q, CancellationToken ct)
    {
        var query = db.Despachos.AsNoTracking()
            .Include(d => d.Ticket).ThenInclude(t => t.Empleado)
            .Include(d => d.Ticket).ThenInclude(t => t.Vehiculo)
            .Include(d => d.Tanque).Include(d => d.Estacion).Include(d => d.Operador)
            .AsQueryable();

        if (q.FechaDesde.HasValue) query = query.Where(d => d.Fecha >= q.FechaDesde.Value);
        if (q.FechaHasta.HasValue) query = query.Where(d => d.Fecha <= q.FechaHasta.Value);
        if (q.TanqueId.HasValue)   query = query.Where(d => d.TanqueId == q.TanqueId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.Fecha).ThenByDescending(d => d.Hora)
            .Skip((q.Pagina - 1) * q.TamanoPagina).Take(q.TamanoPagina)
            .ToListAsync(ct);

        return new ReportePageResponse("despachos", total, q.Pagina, q.TamanoPagina,
            items.Select(d => (object)new DespachoReporteDto(
                d.Id, d.Fecha, d.Hora,
                $"{d.Ticket.Prefijo}-{d.Ticket.FechaCreacion.Year}-{d.Ticket.NumeroSecuencial:000000}",
                d.Ticket.Empleado.NombreCompleto, d.Ticket.Vehiculo.Placa,
                d.GalonesServidos, d.Tanque.Identificacion, d.Estacion.Nombre,
                d.Operador.NombreUsuario, d.InventarioRestante)).ToList());
    }

    private async Task<ReportePageResponse> GetInventarioAsync(ReporteQuery q, CancellationToken ct)
    {
        var query = db.MovimientosInventario.AsNoTracking()
            .Include(m => m.Tanque).ThenInclude(t => t.TipoCombustible)
            .AsQueryable();

        if (q.FechaDesde.HasValue)
            query = query.Where(m => DateOnly.FromDateTime(m.FechaHora) >= q.FechaDesde.Value);
        if (q.FechaHasta.HasValue)
            query = query.Where(m => DateOnly.FromDateTime(m.FechaHora) <= q.FechaHasta.Value);
        if (q.TanqueId.HasValue)
            query = query.Where(m => m.TanqueId == q.TanqueId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(m => m.FechaHora)
            .Skip((q.Pagina - 1) * q.TamanoPagina).Take(q.TamanoPagina)
            .ToListAsync(ct);

        return new ReportePageResponse("inventario", total, q.Pagina, q.TamanoPagina,
            items.Select(m => (object)new MovimientoReporteDto(
                m.Id, m.FechaHora, m.Tanque.Identificacion,
                m.Tanque.TipoCombustible.Nombre, m.Tipo, m.Volumen,
                m.ReferenciaOperacion)).ToList());
    }

    private async Task<ReportePageResponse> GetCierresAsync(ReporteQuery q, CancellationToken ct)
    {
        var query = db.CierresDiarios.AsNoTracking()
            .Include(c => c.CreadoPor)
            .AsQueryable();

        if (q.FechaDesde.HasValue) query = query.Where(c => c.Fecha >= q.FechaDesde.Value);
        if (q.FechaHasta.HasValue) query = query.Where(c => c.Fecha <= q.FechaHasta.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.Fecha)
            .Skip((q.Pagina - 1) * q.TamanoPagina).Take(q.TamanoPagina)
            .ToListAsync(ct);

        return new ReportePageResponse("cierres", total, q.Pagina, q.TamanoPagina,
            items.Select(c => (object)new CierreReporteDto(
                c.Id, c.Fecha, c.TotalDespachos, c.VolumenDespachado,
                c.InventarioFinal, c.Diferencias, c.CreadoPor.NombreUsuario)).ToList());
    }

    // ── Exportación ───────────────────────────────────────────────────────

    private static byte[] ExportarCsv(ReportePageResponse page)
    {
        var sb = new StringBuilder();
        switch (page.Tipo)
        {
            case "solicitudes":
                sb.AppendLine("Id,FechaSolicitud,Empleado,Vehiculo,Departamento,TipoCombustible,CantSolicitada,CantAutorizada,Estado");
                foreach (SolicitudReporteDto s in page.Items)
                    sb.AppendLine($"{s.Id},{s.FechaSolicitud:O},{Csv(s.Empleado)},{Csv(s.Vehiculo)},{Csv(s.Departamento)},{Csv(s.TipoCombustible)},{s.CantidadSolicitada},{s.CantidadAutorizada},{s.Estado}");
                break;
            case "despachos":
                sb.AppendLine("Id,Fecha,Hora,CodigoTicket,Empleado,Vehiculo,GalonesServidos,Tanque,Estacion,Operador,InventarioRestante");
                foreach (DespachoReporteDto d in page.Items)
                    sb.AppendLine($"{d.Id},{d.Fecha},{d.Hora},{Csv(d.CodigoTicket)},{Csv(d.Empleado)},{Csv(d.Vehiculo)},{d.GalonesServidos},{Csv(d.Tanque)},{Csv(d.Estacion)},{Csv(d.Operador)},{d.InventarioRestante}");
                break;
            case "inventario":
                sb.AppendLine("Id,FechaHora,Tanque,TipoCombustible,Tipo,Volumen,Referencia");
                foreach (MovimientoReporteDto m in page.Items)
                    sb.AppendLine($"{m.Id},{m.FechaHora:O},{Csv(m.Tanque)},{Csv(m.TipoCombustible)},{m.Tipo},{m.Volumen},{Csv(m.ReferenciaOperacion ?? "")}");
                break;
            case "cierres":
                sb.AppendLine("Id,Fecha,TotalDespachos,VolumenDespachado,InventarioFinal,Diferencias,CreadoPor");
                foreach (CierreReporteDto c in page.Items)
                    sb.AppendLine($"{c.Id},{c.Fecha},{c.TotalDespachos},{c.VolumenDespachado},{c.InventarioFinal},{c.Diferencias},{Csv(c.CreadoPor)}");
                break;
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] ExportarExcel(ReportePageResponse page)
    {
        using var wb = new XLWorkbook();
        var sheetName = page.Tipo switch
        {
            "solicitudes" => "Solicitudes",
            "despachos"   => "Despachos",
            "inventario"  => "Inventario",
            "cierres"     => "Cierres",
            _             => "Reporte"
        };
        var ws = wb.Worksheets.Add(sheetName);

        string[] headers = page.Tipo switch
        {
            "solicitudes" => ["Id", "FechaSolicitud", "Empleado", "Vehículo", "Departamento", "TipoCombustible", "CantSolicitada", "CantAutorizada", "Estado"],
            "despachos"   => ["Id", "Fecha", "Hora", "CódigoTicket", "Empleado", "Vehículo", "GalonesServidos", "Tanque", "Estación", "Operador", "InvRestante"],
            "inventario"  => ["Id", "FechaHora", "Tanque", "TipoCombustible", "Tipo", "Volumen", "Referencia"],
            "cierres"     => ["Id", "Fecha", "TotalDespachos", "VolumenDespachado", "InventarioFinal", "Diferencias", "CreadoPor"],
            _             => []
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        switch (page.Tipo)
        {
            case "solicitudes":
                foreach (SolicitudReporteDto s in page.Items)
                {
                    ws.Cell(row, 1).Value = s.Id; ws.Cell(row, 2).Value = s.FechaSolicitud.ToString("O");
                    ws.Cell(row, 3).Value = s.Empleado; ws.Cell(row, 4).Value = s.Vehiculo;
                    ws.Cell(row, 5).Value = s.Departamento; ws.Cell(row, 6).Value = s.TipoCombustible;
                    ws.Cell(row, 7).Value = (double)s.CantidadSolicitada;
                    ws.Cell(row, 8).Value = s.CantidadAutorizada.HasValue ? (XLCellValue)(double)s.CantidadAutorizada.Value : (XLCellValue)"";
                    ws.Cell(row, 9).Value = s.Estado.ToString(); row++;
                }
                break;
            case "despachos":
                foreach (DespachoReporteDto d in page.Items)
                {
                    ws.Cell(row, 1).Value = d.Id; ws.Cell(row, 2).Value = d.Fecha.ToString();
                    ws.Cell(row, 3).Value = d.Hora.ToString(); ws.Cell(row, 4).Value = d.CodigoTicket;
                    ws.Cell(row, 5).Value = d.Empleado; ws.Cell(row, 6).Value = d.Vehiculo;
                    ws.Cell(row, 7).Value = (double)d.GalonesServidos; ws.Cell(row, 8).Value = d.Tanque;
                    ws.Cell(row, 9).Value = d.Estacion; ws.Cell(row, 10).Value = d.Operador;
                    ws.Cell(row, 11).Value = (double)d.InventarioRestante; row++;
                }
                break;
            case "inventario":
                foreach (MovimientoReporteDto m in page.Items)
                {
                    ws.Cell(row, 1).Value = m.Id; ws.Cell(row, 2).Value = m.FechaHora.ToString("O");
                    ws.Cell(row, 3).Value = m.Tanque; ws.Cell(row, 4).Value = m.TipoCombustible;
                    ws.Cell(row, 5).Value = m.Tipo.ToString(); ws.Cell(row, 6).Value = (double)m.Volumen;
                    ws.Cell(row, 7).Value = m.ReferenciaOperacion ?? ""; row++;
                }
                break;
            case "cierres":
                foreach (CierreReporteDto c in page.Items)
                {
                    ws.Cell(row, 1).Value = c.Id; ws.Cell(row, 2).Value = c.Fecha.ToString();
                    ws.Cell(row, 3).Value = c.TotalDespachos; ws.Cell(row, 4).Value = (double)c.VolumenDespachado;
                    ws.Cell(row, 5).Value = (double)c.InventarioFinal; ws.Cell(row, 6).Value = (double)c.Diferencias;
                    ws.Cell(row, 7).Value = c.CreadoPor; row++;
                }
                break;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] ExportarPdf(ReportePageResponse page)
        => Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4.Landscape());
            p.Margin(24);
            p.DefaultTextStyle(s => s.FontSize(8));

            p.Header().Column(col =>
            {
                col.Item().Text($"FuelTrack — Reporte: {page.Tipo}")
                    .SemiBold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text(
                    $"Total: {page.Total} registros · Generado: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            p.Content().PaddingVertical(10).Table(t =>
            {
                switch (page.Tipo)
                {
                    case "solicitudes":
                        DefinirColumnasYFilas(t,
                            ["#", "Fecha", "Empleado", "Vehículo", "Depto.", "Combustible", "Solicitado", "Autorizado", "Estado"],
                            [1, 2, 3, 3, 2, 2, 2, 2, 2],
                            page.Items.Cast<SolicitudReporteDto>().Select(s => new[]
                            {
                                s.Id.ToString(), s.FechaSolicitud.ToString("yyyy-MM-dd"),
                                s.Empleado, s.Vehiculo, s.Departamento, s.TipoCombustible,
                                s.CantidadSolicitada.ToString("F4"),
                                s.CantidadAutorizada?.ToString("F4") ?? "—",
                                s.Estado.ToString()
                            }));
                        break;

                    case "despachos":
                        DefinirColumnasYFilas(t,
                            ["#", "Fecha", "Hora", "Ticket", "Empleado", "Vehículo", "Galones", "Tanque", "Estación", "Operador", "Inv.Rest."],
                            [1, 2, 2, 3, 3, 2, 2, 2, 2, 2, 2],
                            page.Items.Cast<DespachoReporteDto>().Select(d => new[]
                            {
                                d.Id.ToString(), d.Fecha.ToString(), d.Hora.ToString("HH:mm"),
                                d.CodigoTicket, d.Empleado, d.Vehiculo,
                                d.GalonesServidos.ToString("F4"), d.Tanque,
                                d.Estacion, d.Operador, d.InventarioRestante.ToString("F4")
                            }));
                        break;

                    case "inventario":
                        DefinirColumnasYFilas(t,
                            ["#", "FechaHora", "Tanque", "Combustible", "Tipo", "Volumen", "Referencia"],
                            [1, 3, 2, 2, 2, 2, 3],
                            page.Items.Cast<MovimientoReporteDto>().Select(m => new[]
                            {
                                m.Id.ToString(), m.FechaHora.ToString("yyyy-MM-dd HH:mm"),
                                m.Tanque, m.TipoCombustible, m.Tipo.ToString(),
                                m.Volumen.ToString("F4"), m.ReferenciaOperacion ?? "—"
                            }));
                        break;

                    case "cierres":
                        DefinirColumnasYFilas(t,
                            ["#", "Fecha", "Despachos", "Vol.Despachado", "Inv.Final", "Diferencias", "Creado por"],
                            [1, 2, 2, 3, 3, 3, 3],
                            page.Items.Cast<CierreReporteDto>().Select(c => new[]
                            {
                                c.Id.ToString(), c.Fecha.ToString(),
                                c.TotalDespachos.ToString(), c.VolumenDespachado.ToString("F4"),
                                c.InventarioFinal.ToString("F4"), c.Diferencias.ToString("F4"),
                                c.CreadoPor
                            }));
                        break;
                }
            });

            p.Footer().AlignCenter().Text(t => { t.Span("FuelTrack · "); t.CurrentPageNumber(); });
        })).GeneratePdf();

    private static void DefinirColumnasYFilas(
        QuestPDF.Fluent.TableDescriptor t,
        string[] headers, int[] pesos,
        IEnumerable<string[]> filas)
    {
        t.ColumnsDefinition(c =>
        {
            foreach (var peso in pesos) c.RelativeColumn(peso);
        });

        // Encabezados — una sola llamada a Header con todas las celdas dentro
        t.Header(hdr =>
        {
            foreach (var h in headers)
                hdr.Cell().Background(Colors.Blue.Darken2)
                    .Padding(4).Text(h).FontColor(Colors.White).Bold().FontSize(8);
        });

        // Filas
        var rowIndex = 0;
        foreach (var fila in filas)
        {
            var bg = rowIndex++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
            foreach (var celda in fila)
                t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .Padding(3).Text(celda).FontSize(8);
        }
    }

    private static void ValidarTipo(string tipo)
    {
        if (!TiposValidos.Contains(tipo))
            throw new TicketDomainException(400, "TIPO_REPORTE_INVALIDO",
                "Tipo no válido. Use: solicitudes, despachos, inventario, cierres.");
    }

    private static string Csv(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
}
