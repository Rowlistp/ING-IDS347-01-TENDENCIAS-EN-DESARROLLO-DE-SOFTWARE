using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.DTOs.Reportes;

public sealed record MovimientoReporteDto(
    int Id,
    DateTime FechaHora,
    string Tanque,
    string TipoCombustible,
    TipoMovimiento Tipo,
    decimal Volumen,
    string? ReferenciaOperacion);
