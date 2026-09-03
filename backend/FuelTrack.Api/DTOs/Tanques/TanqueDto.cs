namespace FuelTrack.Api.DTOs.Tanques;

public record TanqueDto(
    int Id,
    string Identificacion,
    decimal Capacidad,
    decimal NivelActual,
    decimal NivelCritico,
    int TipoCombustibleId,
    string TipoCombustibleNombre,
    bool Activo
);
