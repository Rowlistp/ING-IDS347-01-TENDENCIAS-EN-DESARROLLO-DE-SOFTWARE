using System.Text.Json;
using FuelTrack.Api.Data;
using FuelTrack.Api.Models;

namespace FuelTrack.Api.Services;

public sealed class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
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
