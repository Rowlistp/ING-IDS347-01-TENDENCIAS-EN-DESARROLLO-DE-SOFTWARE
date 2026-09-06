using System.Data;
using FuelTrack.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FuelTrack.Api.Services;

public sealed class TicketNumberService(AppDbContext db)
{
    private static readonly SemaphoreSlim NonPostgreSqlLock = new(1, 1);

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
