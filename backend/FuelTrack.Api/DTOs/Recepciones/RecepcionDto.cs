namespace FuelTrack.Api.DTOs.Recepciones;

public record RecepcionDto(
    int Id,
    string NumeroFactura,
    decimal VolumenRecibido,
    DateTime Fecha,
    int ProveedorId, string ProveedorNombre,
    int TanqueId, string TanqueIdentificacion
);
