namespace FuelTrack.Api.DTOs.Reportes;

public sealed record DespachoReporteDto(
    int Id,
    DateOnly Fecha,
    TimeOnly Hora,
    string CodigoTicket,
    string Empleado,
    string Vehiculo,
    decimal GalonesServidos,
    string Tanque,
    string Estacion,
    string Operador,
    decimal InventarioRestante);
