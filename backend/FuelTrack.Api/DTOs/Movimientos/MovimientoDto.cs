using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Movimientos;

public record MovimientoDto(
    int Id,
    TipoMovimiento Tipo,
    decimal Volumen,
    DateTime FechaHora,
    string? ReferenciaOperacion,
    string? Observaciones,
    int TanqueId, string TanqueIdentificacion,
    int UsuarioId, string UsuarioNombreUsuario
);
