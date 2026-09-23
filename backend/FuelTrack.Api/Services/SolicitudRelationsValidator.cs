using FuelTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Services;

public static class SolicitudRelationsValidator
{
    public sealed record Error(string Code, string Message);

    public static async Task<Error?> ValidateAsync(AppDbContext db, int empleadoId, int vehiculoId,
        int departamentoId, int tipoId, CancellationToken ct)
    {
        var empleado = await db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == empleadoId, ct);
        if (empleado is null || !empleado.Activo)
            return new("EMPLEADO_NO_DISPONIBLE", "Seleccione un empleado activo.");
        var vehiculo = await db.Vehiculos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehiculoId, ct);
        if (vehiculo is null || !vehiculo.Activo)
            return new("VEHICULO_NO_DISPONIBLE", "Seleccione un vehículo activo.");
        if (!await db.Departamentos.AnyAsync(d => d.Id == departamentoId && d.Activo, ct))
            return new("DEPARTAMENTO_NO_DISPONIBLE", "Seleccione un departamento activo.");
        if (!await db.TiposCombustible.AnyAsync(t => t.Id == tipoId && t.Activo, ct))
            return new("COMBUSTIBLE_NO_DISPONIBLE", "Seleccione un tipo de combustible activo.");
        if (empleado.DepartamentoId != departamentoId || vehiculo.DepartamentoId != departamentoId)
            return new("SOLICITUD_RELACIONES_INVALIDAS", "El empleado y el vehículo deben pertenecer al departamento de la solicitud.");
        return null;
    }
}
