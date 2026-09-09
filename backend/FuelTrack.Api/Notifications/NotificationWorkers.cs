using FuelTrack.Api.Models;
using FuelTrack.Api.Services;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Notifications;

public sealed class NotificationDeliveryWorker(IServiceScopeFactory scopes, IOptions<NotificationOptions> options, ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Notification delivery cycle failed; leases will recover. No provider details logged."); }
            await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
        }
    }
    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var batch = await scope.ServiceProvider.GetRequiredService<NotificationDeliveryService>().ClaimAsync(ct);
        // Start the entire bounded batch immediately: no claims expire waiting in a local sequential queue.
        await Task.WhenAll(batch.Select(n => DeliverAsync(n, ct)));
    }
    private async Task DeliverAsync(Notificacion claim, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TransportTimeoutSeconds));
        DeliveryResult result;
        try
        {
            if (claim.Intentos > options.Value.MaxAttempts) result = DeliveryResult.Error("MAX_INTENTOS_RECUPERACION", false);
            else
            {
                var content = await scope.ServiceProvider.GetRequiredService<NotificationContentService>().ComposeAsync(claim, timeout.Token);
                result = claim.Canal == "EMAIL" ? await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(content, timeout.Token)
                    : await scope.ServiceProvider.GetRequiredService<ISmsSender>().SendAsync(content, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
        catch (TicketDomainException) { result = DeliveryResult.Error("TICKET_NO_DISPONIBLE", false); }
        catch (Exception) { result = DeliveryResult.Error("ENTREGA_TRANSITORIA", true); }
        // New scope: failed composition cannot accidentally flush stale tracked writes.
        using var completion = scopes.CreateScope();
        await completion.ServiceProvider.GetRequiredService<NotificationDeliveryService>().CompleteAsync(claim, result, ct);
    }
}

public sealed class NotificationRuleWorker(IServiceScopeFactory scopes, IOptions<NotificationOptions> options, ILogger<NotificationRuleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NotificationRuleService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Notification rule cycle failed; next cycle will retry safely."); }
            await Task.Delay(TimeSpan.FromSeconds(options.Value.RuleIntervalSeconds), stoppingToken);
        }
    }
}
