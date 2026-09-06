using System.Text.Json;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Audit;
using FuelTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public sealed class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuditPageResponse> GetPageAsync(
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Auditorias.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var elementos = await query
            .OrderByDescending(a => a.FechaHora)
            .ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(a => new AuditEntryResponse
            {
                Id = a.Id,
                Evento = a.Evento,
                EntidadAfectada = a.EntidadAfectada,
                IdentificadorRegistro = a.IdentificadorRegistro,
                FechaHoraUtc = a.FechaHora,
                DireccionIp = a.DireccionIp,
                UsuarioId = a.UsuarioId
            })
            .ToListAsync(cancellationToken);

        return new AuditPageResponse
        {
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            Total = total,
            Elementos = elementos
        };
    }

    public async Task WriteAsync(
        string evento,
        string entidad,
        string identificador,
        int? usuarioId,
        string? ip,
        object? datos = null,
        CancellationToken cancellationToken = default)
    {
        _db.Auditorias.Add(new Auditoria
        {
            Evento = evento,
            EntidadAfectada = entidad,
            IdentificadorRegistro = identificador,
            UsuarioId = usuarioId,
            DireccionIp = ip,
            FechaHora = DateTime.UtcNow,
            DatosRelevantes = datos is null ? null : JsonSerializer.Serialize(datos)
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
