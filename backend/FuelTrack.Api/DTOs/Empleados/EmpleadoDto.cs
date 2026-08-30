namespace FuelTrack.Api.DTOs.Empleados;

public record EmpleadoDto(
    int Id,
    string Codigo,
    string NombreCompleto,
    string Cedula,
    string Cargo,
    string Correo,
    string Telefono,
    int DepartamentoId,
    string DepartamentoNombre,
    bool Activo
);
