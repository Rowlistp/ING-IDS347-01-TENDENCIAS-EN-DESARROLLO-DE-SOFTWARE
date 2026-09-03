namespace FuelTrack.Api.DTOs.Inventario;

public record TransferenciaResultDto(
    InventarioDto Origen,
    InventarioDto Destino
);
