namespace FuelTrack.Api.DTOs.Inventario;

public record InventarioDto(
    int Id,
    decimal ExistenciaActual,
    decimal Disponibilidad,
    DateTime UltimaActualizacion,
    int TanqueId, string TanqueIdentificacion, decimal TanqueCapacidad
);
