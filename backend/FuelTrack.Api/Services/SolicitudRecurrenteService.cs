using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class SolicitudRecurrenteService(IServiceScopeFactory scopeFactory, ILogger<SolicitudRecurrenteService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var ahora = DateTime.UtcNow;
            var proximaEjecucion = ahora.Date.AddDays(1); // siguiente medianoche UTC
            var delay = proximaEjecucion - ahora;

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;
            await ProcesarPlantillasAsync(stoppingToken);
        }
    }

    public async Task ProcesarPlantillasAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var plantillas = await db.SolicitudesRecurrentes
            .Where(s => s.Activa &&
                        s.FechaInicio <= hoy &&
                        (s.FechaFin == null || s.FechaFin >= hoy))
            .ToListAsync(ct);

        var generadas = 0;
        foreach (var plantilla in plantillas)
        {
            if (!DebeEjecutarse(plantilla, hoy)) continue;

            db.SolicitudesCombustible.Add(new SolicitudCombustible
            {
                CantidadSolicitada = plantilla.CantidadSolicitada,
                TipoSolicitud = "Automatica",
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                EmpleadoId = plantilla.EmpleadoId,
                VehiculoId = plantilla.VehiculoId,
                DepartamentoId = plantilla.DepartamentoId,
                TipoCombustibleId = plantilla.TipoCombustibleId
            });

            plantilla.UltimaEjecucion = hoy;
            db.Entry(plantilla).Property(p => p.UltimaEjecucion).IsModified = true;
            generadas++;
        }

        if (generadas > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Solicitudes recurrentes generadas: {Count} para {Fecha}", generadas, hoy);
        }
    }

    private static bool DebeEjecutarse(SolicitudRecurrente plantilla, DateOnly hoy)
    {
        var ultima = plantilla.UltimaEjecucion;

        // Primera ejecución
        if (ultima is null) return hoy >= plantilla.FechaInicio;

        return plantilla.Periodicidad switch
        {
            Periodicidad.Diaria   => ultima.Value.AddDays(1) <= hoy,
            Periodicidad.Semanal  => ultima.Value.AddDays(7) <= hoy,
            Periodicidad.Mensual  => ultima.Value.AddMonths(1) <= hoy,
            _                     => false
        };
    }
}
