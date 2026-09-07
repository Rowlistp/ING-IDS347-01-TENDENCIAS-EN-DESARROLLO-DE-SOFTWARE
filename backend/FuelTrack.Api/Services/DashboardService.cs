using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Dashboard;
using FuelTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class DashboardService(AppDbContext db)
{
    public async Task<DashboardResumenResponse> GetResumenAsync(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioMesActual = new DateOnly(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMesActual.AddMonths(-1);
        var finMesAnterior = inicioMesActual.AddDays(-1);
        var hace30Dias = hoy.AddDays(-29);

        var hoyData = await GetHoyAsync(hoy, ct);
        var ultimos7 = await GetUltimos7DiasAsync(hoy, ct);
        var top3 = await GetTop3TanquesAsync(hace30Dias, hoy, ct);
        var comparativa = await GetComparativaAsync(inicioMesActual, hoy, inicioMesAnterior, finMesAnterior, ct);
        var distribucion = await GetDistribucionAsync(hace30Dias, hoy, ct);
        var eficiencia = await GetEficienciaAsync(inicioMesActual, hoy, ct);

        return new DashboardResumenResponse(hoyData, ultimos7, top3, comparativa, distribucion, eficiencia);
    }

    private async Task<DashboardHoy> GetHoyAsync(DateOnly hoy, CancellationToken ct)
    {
        var totalDespachos = await db.Despachos.CountAsync(d => d.Fecha == hoy, ct);
        var volumen = await db.Despachos
            .Where(d => d.Fecha == hoy)
            .SumAsync(d => (decimal?)d.GalonesServidos, ct) ?? 0m;
        var pendientes = await db.SolicitudesCombustible
            .CountAsync(s => s.Estado == EstadoSolicitud.Pendiente, ct);
        var inventarioBajo = await db.Inventarios
            .Include(i => i.Tanque)
            .CountAsync(i => i.ExistenciaActual <= i.Tanque.NivelCritico, ct);

        return new DashboardHoy(totalDespachos, volumen, pendientes, inventarioBajo);
    }

    private async Task<IReadOnlyList<DashboardDia>> GetUltimos7DiasAsync(DateOnly hoy, CancellationToken ct)
    {
        var inicio = hoy.AddDays(-6);
        var datos = await db.Despachos
            .Where(d => d.Fecha >= inicio && d.Fecha <= hoy)
            .GroupBy(d => d.Fecha)
            .Select(g => new { Fecha = g.Key, Volumen = g.Sum(d => d.GalonesServidos), Count = g.Count() })
            .ToListAsync(ct);

        return Enumerable.Range(0, 7)
            .Select(i =>
            {
                var fecha = inicio.AddDays(i);
                var dato = datos.FirstOrDefault(d => d.Fecha == fecha);
                return new DashboardDia(fecha, dato?.Volumen ?? 0m, dato?.Count ?? 0);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardTanque>> GetTop3TanquesAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var raw = await db.Despachos
            .Where(d => d.Fecha >= desde && d.Fecha <= hasta)
            .Select(d => new { d.TanqueId, d.Tanque.Identificacion, d.GalonesServidos })
            .ToListAsync(ct);

        return raw
            .GroupBy(d => new { d.TanqueId, d.Identificacion })
            .Select(g => new DashboardTanque(g.Key.TanqueId, g.Key.Identificacion, g.Sum(d => d.GalonesServidos)))
            .OrderByDescending(x => x.TotalGalones)
            .Take(3)
            .ToList();
    }

    private async Task<DashboardComparativaMes> GetComparativaAsync(
        DateOnly inicioActual, DateOnly finActual,
        DateOnly inicioAnterior, DateOnly finAnterior,
        CancellationToken ct)
    {
        var volActual = await db.Despachos
            .Where(d => d.Fecha >= inicioActual && d.Fecha <= finActual)
            .SumAsync(d => (decimal?)d.GalonesServidos, ct) ?? 0m;
        var solActual = await db.SolicitudesCombustible
            .CountAsync(s => DateOnly.FromDateTime(s.FechaSolicitud) >= inicioActual &&
                             DateOnly.FromDateTime(s.FechaSolicitud) <= finActual, ct);

        var volAnterior = await db.Despachos
            .Where(d => d.Fecha >= inicioAnterior && d.Fecha <= finAnterior)
            .SumAsync(d => (decimal?)d.GalonesServidos, ct) ?? 0m;
        var solAnterior = await db.SolicitudesCombustible
            .CountAsync(s => DateOnly.FromDateTime(s.FechaSolicitud) >= inicioAnterior &&
                             DateOnly.FromDateTime(s.FechaSolicitud) <= finAnterior, ct);

        return new DashboardComparativaMes(
            new DashboardMes(volActual, solActual),
            new DashboardMes(volAnterior, solAnterior));
    }

    private async Task<IReadOnlyList<DashboardDistribucion>> GetDistribucionAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var datos = await db.Despachos
            .Where(d => d.Fecha >= desde && d.Fecha <= hasta)
            .GroupBy(d => d.Tanque.TipoCombustible.Nombre)
            .Select(g => new { Tipo = g.Key, Total = g.Sum(d => d.GalonesServidos) })
            .ToListAsync(ct);

        var totalGlobal = datos.Sum(d => d.Total);
        if (totalGlobal == 0) return [];

        return datos
            .Select(d => new DashboardDistribucion(d.Tipo, Math.Round(d.Total * 100 / totalGlobal, 1)))
            .OrderByDescending(d => d.Porcentaje)
            .ToList();
    }

    private async Task<DashboardEficiencia> GetEficienciaAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var aprobadas = await db.SolicitudesCombustible
            .CountAsync(s => s.Estado == EstadoSolicitud.Aprobada &&
                DateOnly.FromDateTime(s.FechaSolicitud) >= desde &&
                DateOnly.FromDateTime(s.FechaSolicitud) <= hasta, ct);
        var rechazadas = await db.SolicitudesCombustible
            .CountAsync(s => s.Estado == EstadoSolicitud.Rechazada &&
                DateOnly.FromDateTime(s.FechaSolicitud) >= desde &&
                DateOnly.FromDateTime(s.FechaSolicitud) <= hasta, ct);
        var pendientes = await db.SolicitudesCombustible
            .CountAsync(s => s.Estado == EstadoSolicitud.Pendiente &&
                DateOnly.FromDateTime(s.FechaSolicitud) >= desde &&
                DateOnly.FromDateTime(s.FechaSolicitud) <= hasta, ct);

        var total = aprobadas + rechazadas;
        var tasa = total == 0 ? 0m : Math.Round(aprobadas * 100m / total, 1);

        return new DashboardEficiencia(aprobadas, rechazadas, pendientes, tasa);
    }
}
