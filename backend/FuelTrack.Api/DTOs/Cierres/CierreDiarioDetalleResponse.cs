namespace FuelTrack.Api.DTOs.Cierres;

public sealed record CierreDiarioDetalleResponse(
    int TanqueId,
    string TanqueIdentificacion,
    string TipoCombustible,
    int NumeroDespachos,
    decimal VolumenDespachado,
    decimal VolumenRecibido,
    decimal InventarioInicial,
    decimal InventarioFinal,
    decimal Diferencias);
