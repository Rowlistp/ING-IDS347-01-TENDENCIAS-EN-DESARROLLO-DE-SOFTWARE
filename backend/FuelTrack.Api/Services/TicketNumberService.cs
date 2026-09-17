using System.Data;
using FuelTrack.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FuelTrack.Api.Services;

public sealed class TicketNumberService(AppDbContext db)
{
    private static readonly SemaphoreSlim NonPostgreSqlLock = new(1, 1);

    // nextval() no es transaccional: si la creación del Ticket falla después de
    // reservar el número (rollback, DbUpdateException), el número se pierde y
    // queda un hueco permanente. RF-08 solo exige unicidad, no correlatividad;
    // ver docs/20-DECISIONES-TECNICAS.md, sección "Límites".
    public async Task<int> NextAsync(CancellationToken cancellationToken)
    {
        if (!db.Database.IsNpgsql())
        {
            await NonPostgreSqlLock.WaitAsync(cancellationToken);
            try
            {
                return (await db.Tickets.MaxAsync(ticket => (int?)ticket.NumeroSecuencial, cancellationToken) ?? 0) + 1;
            }
            finally
            {
                NonPostgreSqlLock.Release();
            }
        }

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT nextval('ticket_numero_seq')";
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return checked(Convert.ToInt32(value));
    }
}
