using System.ComponentModel.DataAnnotations;

namespace FuelTrack.Api.DTOs.Solicitudes;

public record AprobarSolicitudRequest(
    [Required, Range(0.0001, 999999.9999)] decimal CantidadAutorizada
);
