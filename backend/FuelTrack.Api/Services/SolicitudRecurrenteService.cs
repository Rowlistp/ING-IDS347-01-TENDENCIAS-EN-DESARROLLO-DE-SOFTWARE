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

            var error = await SolicitudRelationsValidator.ValidateAsync(db, plantilla.EmpleadoId, plantilla.VehiculoId,
                plantilla.DepartamentoId, plantilla.TipoCombustibleId, ct);
            if (error is not null)
            {
                logger.LogWarning("Plantilla {Id} omitida: {Code}", plantilla.Id, error.Code);
                continue;
            }

            var cantidad = plantilla.CantidadSolicitada;
            if (plantilla.UsarConsumoHistorico)
            {
                var desde = hoy.AddDays(-90);
                var historico = await db.Despachos
                    .Where(d => d.Ticket.VehiculoId == plantilla.VehiculoId &&
                        d.Ticket.TipoCombustibleId == plantilla.TipoCombustibleId &&
                        d.Fecha >= desde && d.Fecha < hoy && d.GalonesServidos > 0)
                    .Select(d => d.GalonesServidos).ToListAsync(ct);
                if (historico.Count == 0)
                {
                    logger.LogWarning("Plantilla {Id} omitida: SIN_HISTORICO", plantilla.Id);
                    continue;
                }
                var capacidad = await db.Vehiculos.Where(v => v.Id == plantilla.VehiculoId)
                    .Select(v => v.CapacidadTanque).SingleAsync(ct);
                cantidad = Math.Round(Math.Min(historico.Average(), Math.Min(cantidad, capacidad)), 4);
                if (cantidad <= 0) continue;
            }

            // La reserva y la solicitud se confirman juntas; dos procesos no generan el mismo día.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var reservadas = await db.SolicitudesRecurrentes
                .Where(p => p.Id == plantilla.Id && p.Activa && p.UltimaEjecucion == plantilla.UltimaEjecucion)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.UltimaEjecucion, hoy), ct);
            if (reservadas == 0) continue;

            var solicitud = new SolicitudCombustible
            {
                CantidadSolicitada = cantidad,
                TipoSolicitud = "Automatica",
                Estado = EstadoSolicitud.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                EmpleadoId = plantilla.EmpleadoId,
                VehiculoId = plantilla.VehiculoId,
                DepartamentoId = plantilla.DepartamentoId,
                TipoCombustibleId = plantilla.TipoCombustibleId
            };
            db.SolicitudesCombustible.Add(solicitud);

            await db.SaveChangesAsync(ct);
            db.Auditorias.Add(new Auditoria
            {
                Evento = "SOLICITUD_AUTOMATICA_CREADA", EntidadAfectada = "SolicitudCombustible",
                IdentificadorRegistro = solicitud.Id.ToString(), FechaHora = DateTime.UtcNow,
                DatosRelevantes = System.Text.Json.JsonSerializer.Serialize(new { PlantillaId = plantilla.Id, plantilla.UsarConsumoHistorico, Cantidad = cantidad, Origen = "PROGRAMADOR" })
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            generadas++;
        }

        if (generadas > 0)
        {
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
