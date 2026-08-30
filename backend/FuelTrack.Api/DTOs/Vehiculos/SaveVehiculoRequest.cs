using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Vehiculos;

public record SaveVehiculoRequest(
    [Required, MaxLength(10)]  string Placa,
    [Required, MaxLength(20)]  string Ficha,
    [Required, MaxLength(50)]  string Marca,
    [Required, MaxLength(50)]  string Modelo,
    [Range(1990, 2100)]        int Año,
    [Required, MaxLength(50)]  string Tipo,
    [Required]                 int DepartamentoId,
    [Range(0.0001, 9999.9999)] decimal CapacidadTanque,
    decimal Odometro = 0
);
