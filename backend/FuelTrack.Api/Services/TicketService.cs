using System.Security.Cryptography;
using FuelTrack.Api.Data;
using FuelTrack.Api.DTOs.Tickets;
using FuelTrack.Api.Models;
using FuelTrack.Api.Models.Enums;
using FuelTrack.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FuelTrack.Api.Services;

public sealed record TicketCreationResult(TicketResponse Ticket, string QrPayload);
public sealed record TicketPdfResult(string FileName, byte[] Content);

public sealed class TicketService(
    AppDbContext db,
    TicketNumberService numbers,
    TicketQrService qr,
    TicketPdfService pdf,
    AuditService audit,
    IOptions<TicketOptions> configuredOptions)
{
    private readonly TicketOptions _options = configuredOptions.Value;

    public async Task<IReadOnlyCollection<TicketResponse>> GetAllAsync(CancellationToken cancellationToken, int? ownerUserId = null)
    {
        var tickets = await TicketQuery(asTracking: false)
            .Where(ticket => ownerUserId == null || ticket.Empleado.UsuarioId == ownerUserId)
            .OrderByDescending(ticket => ticket.FechaCreacion)
            .ThenByDescending(ticket => ticket.NumeroSecuencial)
            .ToListAsync(cancellationToken);

        return tickets.Select(ToResponse).ToArray();
    }

    public async Task<TicketResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken, int? ownerUserId = null)
    {
        var ticket = await TicketQuery(asTracking: false)
            .SingleOrDefaultAsync(item => item.Id == id &&
                (ownerUserId == null || item.Empleado.UsuarioId == ownerUserId), cancellationToken);
        return ticket is null ? null : ToResponse(ticket);
    }

    public async Task<TicketCreationResult> CreateAsync(
        CreateTicketRequest request,
        int actorUserId,
        string? ip,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = NormalizeUtc(DateTime.UtcNow);
        var solicitud = await db.SolicitudesCombustible
            .Include(item => item.Empleado)
            .Include(item => item.Vehiculo)
            .Include(item => item.Departamento)
            .Include(item => item.TipoCombustible)
            .SingleOrDefaultAsync(item => item.Id == request.SolicitudId, cancellationToken);

        if (solicitud is null)
            throw Error(404, "SOLICITUD_NO_ENCONTRADA", "La solicitud no existe.");
        if (solicitud.Estado != EstadoSolicitud.Aprobada)
            throw Error(409, "SOLICITUD_NO_APROBADA", "Solo una solicitud aprobada puede emitir un ticket.");
        if (solicitud.CantidadAutorizada is null or <= 0)
            throw Error(400, "CANTIDAD_AUTORIZADA_INVALIDA", "La solicitud no tiene una cantidad autorizada válida.");
        if (solicitud.FechaVencimiento is null)
            throw Error(400, "VENCIMIENTO_REQUERIDO", "La solicitud aprobada debe tener fecha de vencimiento.");

        var expiration = NormalizeUtc(solicitud.FechaVencimiento.Value);
        if (expiration <= now)
            throw Error(400, "VENCIMIENTO_INVALIDO", "La fecha de vencimiento debe ser futura.");

        EnsureRelationsAreUsable(solicitud);

        var previousUsableTickets = await db.Tickets
            .Where(ticket => ticket.SolicitudId == solicitud.Id &&
                ticket.Estado != EstadoTicket.Vencido &&
                ticket.Estado != EstadoTicket.Consumido &&
                ticket.Estado != EstadoTicket.Anulado)
            .ToListAsync(cancellationToken);

        foreach (var previous in previousUsableTickets.Where(ticket => ticket.FechaVencimiento <= now))
            previous.Estado = EstadoTicket.Vencido;

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);

        if (previousUsableTickets.Any(ticket => ticket.Estado is not EstadoTicket.Vencido))
            throw Error(409, "TICKET_UTILIZABLE_EXISTENTE", "La solicitud ya tiene un ticket utilizable.");

        var number = await numbers.NextAsync(cancellationToken);
        var prefix = _options.GetValidatedPrefix(request.Prefijo);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            NumeroSecuencial = number,
            Prefijo = prefix,
            FechaCreacion = now,
            FechaVencimiento = expiration,
            Estado = EstadoTicket.Creado,
            CantidadAutorizada = solicitud.CantidadAutorizada.Value,
            TipoCombustibleId = solicitud.TipoCombustibleId,
            TipoCombustible = solicitud.TipoCombustible,
            EmpleadoId = solicitud.EmpleadoId,
            Empleado = solicitud.Empleado,
            VehiculoId = solicitud.VehiculoId,
            Vehiculo = solicitud.Vehiculo,
            DepartamentoId = solicitud.DepartamentoId,
            Departamento = solicitud.Departamento,
            SolicitudId = solicitud.Id,
            Solicitud = solicitud
        };

        var generatedQr = qr.Generate(
            ticket.Id,
            ticket.NumeroSecuencial,
            ticket.Prefijo,
            solicitud.Id,
            ticket.EmpleadoId,
            ticket.VehiculoId,
            ticket.DepartamentoId,
            ticket.TipoCombustibleId,
            ticket.CantidadAutorizada,
            ticket.FechaCreacion,
            ticket.FechaVencimiento);
        ticket.HashSeguridad = generatedQr.PayloadHash;
        ticket.TokenValidacion = generatedQr.TokenHash;
        ticket.FirmaDigital = generatedQr.Signature;
        ticket.QrCodePng = generatedQr.Png;

        db.Tickets.Add(ticket);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync(
                "TICKET_CREADO",
                "Ticket",
                ticket.Id.ToString("D"),
                actorUserId,
                ip,
                new { ticket.NumeroSecuencial, ticket.Prefijo, ticket.SolicitudId },
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUsableTicketConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw Error(409, "TICKET_UTILIZABLE_EXISTENTE", "La solicitud ya tiene un ticket utilizable.");
        }

        return new TicketCreationResult(ToResponse(ticket), generatedQr.Payload);
    }

    public async Task<TicketValidationResponse> ValidateAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        if (!qr.TryValidate(payload, out var data, out var payloadHash, out var signature) || data is null)
            return TicketValidationResponse.Invalid("QR_INVALIDO", "El QR es inválido o fue alterado.");

        var ticket = await TicketQuery(asTracking: true)
            .SingleOrDefaultAsync(item => item.Id == data.TicketId, cancellationToken);
        if (ticket is null)
            return TicketValidationResponse.Invalid("TICKET_NO_ENCONTRADO", "El ticket no existe.");

        if (!PayloadMatchesTicket(ticket, data, payloadHash, signature))
            return TicketValidationResponse.Invalid("QR_NO_COINCIDE", "El QR no coincide con el ticket registrado.");

        if (ticket.Estado == EstadoTicket.Anulado)
            return TicketValidationResponse.Invalid("TICKET_ANULADO", "El ticket está anulado.");
        if (ticket.Estado == EstadoTicket.Consumido)
            return TicketValidationResponse.Invalid("TICKET_CONSUMIDO", "El ticket ya fue consumido.");

        if (NormalizeUtc(DateTime.UtcNow) >= ticket.FechaVencimiento)
        {
            if (ticket.Estado != EstadoTicket.Vencido)
            {
                ticket.Estado = EstadoTicket.Vencido;
                await db.SaveChangesAsync(cancellationToken);
            }
            return TicketValidationResponse.Invalid("TICKET_VENCIDO", "El ticket está vencido.");
        }

        if (ticket.Estado == EstadoTicket.Vencido)
            return TicketValidationResponse.Invalid("TICKET_VENCIDO", "El ticket está vencido.");

        return TicketValidationResponse.Valid(ToResponse(ticket));
    }

    public async Task<TicketResponse> CancelAsync(
        Guid id,
        string reason,
        int actorUserId,
        string? ip,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await TicketQuery(asTracking: true)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw Error(404, "TICKET_NO_ENCONTRADO", "El ticket no existe.");

        if (ticket.Estado == EstadoTicket.Anulado)
        {
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(ticket);
        }
        if (ticket.Estado == EstadoTicket.Consumido)
            throw Error(409, "TICKET_CONSUMIDO", "Un ticket consumido no puede anularse.");
        if (ticket.Estado == EstadoTicket.Vencido || NormalizeUtc(DateTime.UtcNow) >= ticket.FechaVencimiento)
            throw Error(409, "TICKET_VENCIDO", "Un ticket vencido no puede anularse.");

        ticket.Estado = EstadoTicket.Anulado;
        ticket.MotivoAnulacion = reason.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            "TICKET_ANULADO",
            "Ticket",
            ticket.Id.ToString("D"),
            actorUserId,
            ip,
            new { Motivo = ticket.MotivoAnulacion },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(ticket);
    }

    public async Task<SendTicketResponse> PrepareSendAsync(
        Guid id,
        int actorUserId,
        string? ip,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serializa los preparadores del mismo Ticket entre procesos antes de leer la cola.
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"Tickets\" WHERE \"Id\" = {id} FOR UPDATE", cancellationToken);
        var ticket = await TicketQuery(asTracking: true)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw Error(404, "TICKET_NO_ENCONTRADO", "El ticket no existe.");

        EnsureTicketIsActive(ticket);
        if (ticket.Estado == EstadoTicket.Enviado)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SendTicketResponse(ToResponse(ticket), 0);
        }

        var pending = new List<Notificacion>();
        if (!string.IsNullOrWhiteSpace(ticket.Empleado.Correo))
            pending.Add(CreateNotification(ticket, "EMAIL", ticket.Empleado.Correo));
        if (!string.IsNullOrWhiteSpace(ticket.Empleado.Telefono))
            pending.Add(CreateNotification(ticket, "SMS", ticket.Empleado.Telefono));
        if (pending.Count == 0)
            throw Error(409, "DESTINATARIO_NO_DISPONIBLE", "El empleado no tiene correo ni teléfono disponible.");

        var reference = id.ToString("D");
        var queuedChannels = await db.Notificaciones
            .Where(item => item.Tipo == "TICKET_EMITIDO" && item.ReferenciaEvento == reference && item.Estado == "PENDIENTE")
            .Select(item => item.Canal)
            .ToListAsync(cancellationToken);
        pending.RemoveAll(item => queuedChannels.Contains(item.Canal));
        if (pending.Count == 0 && ticket.Estado == EstadoTicket.Pendiente)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SendTicketResponse(ToResponse(ticket), 0);
        }

        db.Notificaciones.AddRange(pending);
        ticket.Estado = EstadoTicket.Pendiente;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(
            "TICKET_PREPARADO_ENVIO",
            "Ticket",
            ticket.Id.ToString("D"),
            actorUserId,
            ip,
            new { Canales = pending.Select(item => item.Canal).ToArray() },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SendTicketResponse(ToResponse(ticket), pending.Count);
    }

    public async Task<TicketPdfResult> GeneratePdfAsync(
        Guid id,
        int actorUserId,
        string? ip,
        CancellationToken cancellationToken,
        int? ownerUserId = null)
    {
        var ticket = await TicketQuery(asTracking: true)
            .SingleOrDefaultAsync(item => item.Id == id &&
                (ownerUserId == null || item.Empleado.UsuarioId == ownerUserId), cancellationToken)
            ?? throw Error(404, "TICKET_NO_ENCONTRADO", "El ticket no existe.");
        var response = ToResponse(ticket);
        var content = pdf.Generate(response, ticket.QrCodePng);
        await audit.WriteAsync(
            "TICKET_PDF_GENERADO",
            "Ticket",
            ticket.Id.ToString("D"),
            actorUserId,
            ip,
            new { response.Codigo },
            cancellationToken);
        return new TicketPdfResult($"ticket-{response.Codigo}.pdf", content);
    }

    private IQueryable<Ticket> TicketQuery(bool asTracking)
    {
        var query = db.Tickets
            .Include(ticket => ticket.Empleado)
            .Include(ticket => ticket.Vehiculo)
            .Include(ticket => ticket.Departamento)
            .Include(ticket => ticket.TipoCombustible)
            .AsQueryable();
        return asTracking ? query : query.AsNoTracking();
    }

    private static void EnsureRelationsAreUsable(SolicitudCombustible solicitud)
    {
        if (!solicitud.Empleado.Activo)
            throw Error(400, "EMPLEADO_INACTIVO", "El empleado de la solicitud está inactivo.");
        if (!solicitud.Vehiculo.Activo)
            throw Error(400, "VEHICULO_INACTIVO", "El vehículo de la solicitud está inactivo.");
        if (!solicitud.Departamento.Activo)
            throw Error(400, "DEPARTAMENTO_INACTIVO", "El departamento de la solicitud está inactivo.");
        if (!solicitud.TipoCombustible.Activo)
            throw Error(400, "COMBUSTIBLE_INACTIVO", "El tipo de combustible está inactivo.");
        if (solicitud.Empleado.DepartamentoId != solicitud.DepartamentoId ||
            solicitud.Vehiculo.DepartamentoId != solicitud.DepartamentoId)
        {
            throw Error(400, "RELACIONES_SOLICITUD_INVALIDAS", "Empleado, vehículo y departamento no coinciden.");
        }
    }

    private static void EnsureTicketIsActive(Ticket ticket)
    {
        if (ticket.Estado == EstadoTicket.Anulado)
            throw Error(409, "TICKET_ANULADO", "El ticket está anulado.");
        if (ticket.Estado == EstadoTicket.Consumido)
            throw Error(409, "TICKET_CONSUMIDO", "El ticket ya fue consumido.");
        if (ticket.Estado == EstadoTicket.Vencido || NormalizeUtc(DateTime.UtcNow) >= ticket.FechaVencimiento)
            throw Error(409, "TICKET_VENCIDO", "El ticket está vencido.");
    }

    private static Notificacion CreateNotification(Ticket ticket, string channel, string recipient)
        => new()
        {
            Tipo = "TICKET_EMITIDO",
            Canal = channel,
            Estado = "PENDIENTE",
            Destinatario = recipient.Trim(),
            ReferenciaEvento = ticket.Id.ToString("D"),
            FechaHora = DateTime.UtcNow
        };

    private static bool PayloadMatchesTicket(
        Ticket ticket,
        TicketQrData data,
        string payloadHash,
        string signature)
        => ticket.Id == data.TicketId &&
           ticket.NumeroSecuencial == data.NumeroSecuencial &&
           string.Equals(ticket.Prefijo, data.Prefijo, StringComparison.Ordinal) &&
           ticket.SolicitudId == data.SolicitudId &&
           ticket.EmpleadoId == data.EmpleadoId &&
           ticket.VehiculoId == data.VehiculoId &&
           ticket.DepartamentoId == data.DepartamentoId &&
           ticket.TipoCombustibleId == data.TipoCombustibleId &&
           ticket.CantidadAutorizada == data.Cantidad &&
           ticket.FechaCreacion == data.FechaEmisionUtc &&
           ticket.FechaVencimiento == data.FechaExpiracionUtc &&
           FixedTimeHexEquals(ticket.HashSeguridad, payloadHash) &&
           FixedTimeHexEquals(ticket.TokenValidacion, TicketQrService.HashToken(data.Token)) &&
           string.Equals(ticket.FirmaDigital, signature, StringComparison.Ordinal);

    private static bool FixedTimeHexEquals(string left, string right)
    {
        try
        {
            var leftBytes = Convert.FromHexString(left);
            var rightBytes = Convert.FromHexString(right);
            return leftBytes.Length == rightBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static TicketResponse ToResponse(Ticket ticket)
    {
        var effectiveState = NormalizeUtc(DateTime.UtcNow) >= ticket.FechaVencimiento &&
            ticket.Estado is not (EstadoTicket.Consumido or EstadoTicket.Anulado)
                ? EstadoTicket.Vencido
                : ticket.Estado;
        var code = $"{ticket.Prefijo}-{ticket.FechaCreacion.Year}-{ticket.NumeroSecuencial:000000}";
        return new TicketResponse(
            ticket.Id,
            code,
            ticket.NumeroSecuencial,
            ticket.Prefijo,
            ticket.FechaCreacion,
            ticket.FechaVencimiento,
            effectiveState,
            ticket.CantidadAutorizada,
            ticket.EmpleadoId,
            ticket.Empleado.NombreCompleto,
            ticket.VehiculoId,
            ticket.Vehiculo.Placa,
            ticket.DepartamentoId,
            ticket.Departamento.Nombre,
            ticket.TipoCombustibleId,
            ticket.TipoCombustible.Nombre,
            ticket.SolicitudId,
            ticket.QrCodePng.Length > 0,
            ticket.MotivoAnulacion);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return new DateTime(utc.Ticks - utc.Ticks % 10, DateTimeKind.Utc);
    }

    private static bool IsUsableTicketConflict(DbUpdateException exception)
        => exception.InnerException is PostgresException postgres &&
           postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
           string.Equals(postgres.ConstraintName, "UX_Tickets_Solicitud_Utilizable", StringComparison.Ordinal);

    private static TicketDomainException Error(int statusCode, string code, string message)
        => new(statusCode, code, message);
}
