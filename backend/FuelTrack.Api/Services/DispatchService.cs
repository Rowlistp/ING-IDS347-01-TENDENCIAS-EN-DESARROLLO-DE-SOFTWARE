using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Dispatch;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FuelTrack.Api.Services;

public sealed class DispatchService(AppDbContext db, TicketService tickets, AuditService audit)
{
    public async Task<DispatchResponse> CreateAsync(CreateDispatchRequest request, int actorId, string? ip, CancellationToken ct)
    {
        if (request.GalonesServidos <= 0 || decimal.Round(request.GalonesServidos, 4) != request.GalonesServidos)
            throw Error(400, "GALONES_INVALIDOS", "Indique galones positivos con hasta cuatro decimales.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var actor = await db.Usuarios.AsNoTracking().Include(u => u.UsuarioRoles).ThenInclude(r => r.Rol)
                .SingleOrDefaultAsync(u => u.Id == actorId, ct);
            if (actor is null || !actor.Activo)
                throw Error(401, "OPERADOR_INVALIDO", "La sesión del operador no está activa.");
            if (!actor.UsuarioRoles.Any(r => r.Rol.Nombre == Roles.Despachador))
                throw Error(403, "OPERADOR_NO_AUTORIZADO", "Solo un Despachador puede registrar combustible servido.");

            if (db.Database.IsNpgsql())
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tickets\" WHERE \"Id\" = {request.TicketId} FOR UPDATE", ct);
            var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == request.TicketId, ct)
                ?? throw Error(404, "TICKET_NO_ENCONTRADO", "El ticket no existe.");
            // Revalidación dentro del lock: no se confía en el escaneo anterior.
            var validation = await tickets.ValidateAsync(request.QrPayload, ct);
            if (!validation.Valido)
                throw Error(409, validation.Codigo, validation.Mensaje);
            if (validation.Ticket!.Id != request.TicketId)
                throw Error(409, "QR_NO_COINCIDE", "El QR no corresponde al ticket solicitado.");
            if (await db.Despachos.AnyAsync(d => d.TicketId == ticket.Id, ct))
                throw Error(409, "TICKET_CONSUMIDO", "El ticket ya fue utilizado.");
            if (request.GalonesServidos > ticket.CantidadAutorizada)
                throw Error(400, "GALONES_EXCEDEN_AUTORIZACION", "Los galones superan la cantidad autorizada.");

            // Lectura protegida frente a cambios simultáneos de catálogo.
            if (db.Database.IsNpgsql())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tanques\" WHERE \"Id\" = {request.TanqueId} FOR SHARE", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Estaciones\" WHERE \"Id\" = {request.EstacionId} FOR SHARE", ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Inventarios\" WHERE \"TanqueId\" = {request.TanqueId} FOR UPDATE", ct);
            }
            var tank = await db.Tanques.SingleOrDefaultAsync(t => t.Id == request.TanqueId, ct)
                ?? throw Error(404, "TANQUE_NO_ENCONTRADO", "El tanque no existe.");
            if (!tank.Activo) throw Error(409, "TANQUE_INACTIVO", "El tanque está inactivo.");
            if (tank.TipoCombustibleId != ticket.TipoCombustibleId)
                throw Error(409, "COMBUSTIBLE_INCORRECTO", "El tanque no contiene el combustible autorizado.");
            var station = await db.Estaciones.SingleOrDefaultAsync(e => e.Id == request.EstacionId, ct)
                ?? throw Error(404, "ESTACION_NO_ENCONTRADA", "La estación no existe.");
            if (!station.Activo) throw Error(409, "ESTACION_INACTIVA", "La estación está inactiva.");
            var inventory = await db.Inventarios.SingleOrDefaultAsync(i => i.TanqueId == tank.Id, ct)
                ?? throw Error(404, "INVENTARIO_NO_ENCONTRADO", "El tanque no tiene inventario.");
            if (inventory.ExistenciaActual < request.GalonesServidos || inventory.Disponibilidad < request.GalonesServidos)
                throw Error(409, "INVENTARIO_INSUFICIENTE", "No hay combustible suficiente disponible.");

            var now = DateTime.UtcNow;
            if (now >= ticket.FechaVencimiento)
                throw Error(409, "TICKET_VENCIDO", "El ticket venció mientras se esperaba la confirmación.");
            var dispatch = new Despacho
            {
                Ticket = ticket, OperadorId = actorId, Tanque = tank, Estacion = station,
                Fecha = DateOnly.FromDateTime(now), Hora = TimeOnly.FromDateTime(now),
                GalonesServidos = request.GalonesServidos, Observaciones = request.Observaciones?.Trim(),
                InventarioRestante = inventory.ExistenciaActual - request.GalonesServidos,
                DisponibilidadRestante = inventory.Disponibilidad - request.GalonesServidos
            };
            db.Despachos.Add(dispatch);
            ticket.Estado = EstadoTicket.Consumido;
            inventory.ExistenciaActual -= request.GalonesServidos;
            inventory.Disponibilidad -= request.GalonesServidos;
            inventory.UltimaActualizacion = now;
            await db.SaveChangesAsync(ct);
            db.MovimientosInventario.Add(new MovimientoInventario
            {
                Tipo = TipoMovimiento.Salida, Volumen = -request.GalonesServidos, TanqueId = tank.Id,
                UsuarioId = actorId, FechaHora = now, ReferenciaOperacion = $"DESPACHO-{dispatch.Id}/TICKET-{ticket.Id:D}",
                Observaciones = dispatch.Observaciones
            });
            await audit.WriteAsync("DESPACHO_REGISTRADO", "Despacho", dispatch.Id.ToString(), actorId, ip,
                new { dispatch.TicketId, DespachoId = dispatch.Id, dispatch.TanqueId, dispatch.EstacionId,
                    dispatch.GalonesServidos, EstadoTicket = "Consumido" }, ct);
            await transaction.CommitAsync(ct);
            return ToResponse(dispatch, actor.NombreUsuario);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw Error(409, "CONCURRENCIA_CONFLICTO", "Los datos cambiaron. Consulte el ticket antes de confirmar nuevamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Despachos_TicketId" })
        {
            await transaction.RollbackAsync(ct);
            throw Error(409, "TICKET_CONSUMIDO", "El ticket ya tiene un despacho.");
        }
    }

    public async Task<IReadOnlyList<DispatchResponse>> GetAllAsync(int page, int size, int? operatorId, Guid? ticketId, CancellationToken ct)
        => (await Query(operatorId).Where(d => ticketId == null || d.TicketId == ticketId)
            .OrderByDescending(d => d.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct)).Select(d => ToResponse(d, d.Operador.NombreUsuario)).ToArray();

    public async Task<DispatchResponse?> GetByIdAsync(int id, int? operatorId, CancellationToken ct)
    {
        var dispatch = await Query(operatorId).SingleOrDefaultAsync(d => d.Id == id, ct);
        return dispatch is null ? null : ToResponse(dispatch, dispatch.Operador.NombreUsuario);
    }

    private IQueryable<Despacho> Query(int? operatorId)
        => db.Despachos.AsNoTracking().Include(d => d.Ticket).Include(d => d.Tanque).Include(d => d.Estacion).Include(d => d.Operador)
            .Where(d => operatorId == null || d.OperadorId == operatorId);

    private static DispatchResponse ToResponse(Despacho d, string operatorName)
        => new(d.Id, d.TicketId, $"{d.Ticket.Prefijo}-{d.Ticket.FechaCreacion.Year}-{d.Ticket.NumeroSecuencial:000000}",
            d.Fecha, d.Hora, d.GalonesServidos, d.OperadorId, operatorName, d.TanqueId,
            d.Tanque.Identificacion, d.EstacionId, d.Estacion.Nombre, d.InventarioRestante, d.DisponibilidadRestante, d.Ticket.Estado, d.Observaciones);

    private static TicketDomainException Error(int status, string code, string message) => new(status, code, message);
}
