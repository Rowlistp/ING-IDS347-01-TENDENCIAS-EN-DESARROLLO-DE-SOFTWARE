using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FuelTrack.Api.Notifications;

public sealed record OutgoingNotification(int Id, string Key, string To, string Subject, string Text, byte[]? Pdf = null, string? FileName = null);
public sealed record DeliveryResult(bool Success, bool Transient, string Code, string? ProviderId = null)
{
    public static DeliveryResult Sent(string? id = null) => new(true, false, "OK", id);
    public static DeliveryResult Error(string code, bool transient) => new(false, transient, code);
}
public interface IEmailSender { Task<DeliveryResult> SendAsync(OutgoingNotification message, CancellationToken ct); }
public interface ISmsSender { Task<DeliveryResult> SendAsync(OutgoingNotification message, CancellationToken ct); }

public sealed class SmtpEmailSender(IOptions<NotificationOptions> options) : IEmailSender
{
    public MimeMessage BuildMessage(OutgoingNotification input)
    {
        var smtp = options.Value.Smtp;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(input.To));
        message.Subject = input.Subject;
        message.MessageId = $"fueltrack-{input.Id}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Key)))}@fueltrack.invalid";
        var body = new BodyBuilder { TextBody = input.Text };
        if (input.Pdf is not null) body.Attachments.Add(input.FileName ?? "ticket.pdf", input.Pdf, ContentType.Parse("application/pdf"));
        message.Body = body.ToMessageBody();
        return message;
    }
    public async Task<DeliveryResult> SendAsync(OutgoingNotification input, CancellationToken ct)
    {
        if (!NotificationOptions.ValidEmail(input.To)) return DeliveryResult.Error("EMAIL_INVALIDO", false);
        var smtp = options.Value.Smtp;
        if (!smtp.Enabled) return DeliveryResult.Error("SMTP_DESHABILITADO", true);
        try
        {
            // Revocation checking (OCSP/CRL) commonly cannot complete on restrictive networks;
            // MailKit then rejects an otherwise valid certificate. Skip only the revocation
            // check, not chain/hostname validation.
            using var client = new SmtpClient { Timeout = options.Value.TransportTimeoutSeconds * 1000, CheckCertificateRevocation = false };
            using var message = BuildMessage(input);
            await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseSsl ? SecureSocketOptions.SslOnConnect : smtp.StartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
            if (smtp.Username.Length > 0) await client.AuthenticateAsync(smtp.Username, smtp.Password, ct);
            await client.SendAsync(message, ct);
            // Acceptance is success; a QUIT failure must not turn it into an immediate resend.
            return DeliveryResult.Sent(message.MessageId);
        }
        catch (SmtpCommandException e) { return DeliveryResult.Error($"SMTP_{(int)e.StatusCode}", (int)e.StatusCode < 500); }
        catch (MailKit.Security.AuthenticationException) { return DeliveryResult.Error("SMTP_AUTH", false); }
        catch (Exception e) when (e is IOException or System.Net.Sockets.SocketException or OperationCanceledException or SmtpProtocolException)
        { return DeliveryResult.Error("SMTP_CONEXION", true); }
        catch (Exception) { return DeliveryResult.Error("SMTP_CONFIGURACION", false); }
    }
}

public sealed class HttpSmsGatewaySender(HttpClient client, IOptions<NotificationOptions> options) : ISmsSender
{
    public async Task<DeliveryResult> SendAsync(OutgoingNotification input, CancellationToken ct)
    {
        var to = NotificationOptions.NormalizePhone(input.To);
        if (!NotificationOptions.ValidPhone(to)) return DeliveryResult.Error("TELEFONO_INVALIDO", false);
        var sms = options.Value.Sms;
        if (!sms.Enabled) return DeliveryResult.Error("SMS_DESHABILITADO", true);
        var textbee = sms.Provider == "Textbee";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, sms.BaseUrl);
            request.Headers.TryAddWithoutValidation(textbee ? "x-api-key" : sms.AuthHeaderName, sms.ApiKey);
            request.Headers.Add("Idempotency-Key", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Key))));
            request.Content = textbee
                ? JsonContent.Create(new { recipients = new[] { to }, message = input.Text, deviceId = sms.DeviceId })
                : JsonContent.Create(new { to, message = input.Text, reference = input.Key, sender = sms.Sender });
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return DeliveryResult.Error($"SMS_HTTP_{(int)response.StatusCode}", (int)response.StatusCode == 429 || (!textbee && (int)response.StatusCode >= 500));
            // The generic gateway must acknowledge with {"messageId":"opaque-id"}.
            await response.Content.LoadIntoBufferAsync(8192, ct);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (textbee) return ReadTextbeeAcknowledgement(json.RootElement);
            if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("messageId", out var id) || id.ValueKind != JsonValueKind.String ||
                id.GetString() is not { Length: > 0 and <= 128 } value || !System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9_-]+$"))
                return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", true);
            return DeliveryResult.Sent(value);
        }
        // Textbee does not document deduplication by Idempotency-Key. An ambiguous
        // result requires checking the provider history before any manual resend.
        catch (Exception e) when (e is HttpRequestException or IOException or OperationCanceledException) { return DeliveryResult.Error(textbee ? "SMS_TEXTBEE_RESULTADO_INCIERTO" : "SMS_CONEXION", !textbee); }
        catch (JsonException) { return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", !textbee); }
    }

    private static DeliveryResult ReadTextbeeAcknowledgement(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", false);
        if (data.TryGetProperty("success", out var success))
        {
            if (success.ValueKind != JsonValueKind.True ||
                !data.TryGetProperty("recipientCount", out var recipients) || recipients.ValueKind != JsonValueKind.Number || !recipients.TryGetInt32(out var count) || count != 1 ||
                !data.TryGetProperty("smsBatchId", out var batch) || batch.ValueKind != JsonValueKind.String ||
                batch.GetString() is not { Length: > 0 and <= 128 } id ||
                !System.Text.RegularExpressions.Regex.IsMatch(id, "^[A-Za-z0-9_-]+$"))
                return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", false);
            return DeliveryResult.Sent(id);
        }
        if (data.TryGetProperty("successCount", out var sent) && sent.ValueKind == JsonValueKind.Number && sent.TryGetInt32(out var accepted) && accepted == 1 &&
            data.TryGetProperty("failureCount", out var failed) && failed.ValueKind == JsonValueKind.Number && failed.TryGetInt32(out var rejected) && rejected == 0)
            return DeliveryResult.Sent(); // Immediate delivery mode has no batch id.
        return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", false);
    }
}
