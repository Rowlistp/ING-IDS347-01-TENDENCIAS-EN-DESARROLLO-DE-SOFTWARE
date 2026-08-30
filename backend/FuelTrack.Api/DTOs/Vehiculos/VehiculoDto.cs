namespace FuelTrack.Api.DTOs.Vehiculos;

public record VehiculoDto(
    int Id,
    string Placa,
    string Ficha,
    string Marca,
    string Modelo,
    int Año,
    string Tipo,
    decimal CapacidadTanque,
    decimal Odometro,
    int DepartamentoId,
    string DepartamentoNombre,
    bool Activo
);
