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
            using var client = new SmtpClient { Timeout = options.Value.TransportTimeoutSeconds * 1000 };
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
        if (!NotificationOptions.ValidPhone(input.To)) return DeliveryResult.Error("TELEFONO_INVALIDO", false);
        var sms = options.Value.Sms;
        if (!sms.Enabled) return DeliveryResult.Error("SMS_DESHABILITADO", true);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, sms.BaseUrl);
            request.Headers.TryAddWithoutValidation(sms.AuthHeaderName, sms.ApiKey);
            request.Headers.Add("Idempotency-Key", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Key))));
            request.Content = JsonContent.Create(new { to = input.To, message = input.Text, reference = input.Key, sender = sms.Sender });
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return DeliveryResult.Error($"SMS_HTTP_{(int)response.StatusCode}", (int)response.StatusCode == 429 || (int)response.StatusCode >= 500);
            // The generic gateway must acknowledge with {"messageId":"opaque-id"}.
            await response.Content.LoadIntoBufferAsync(8192, ct);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("messageId", out var id) || id.ValueKind != JsonValueKind.String ||
                id.GetString() is not { Length: > 0 and <= 128 } value || !System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9_-]+$"))
                return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", true);
            return DeliveryResult.Sent(value);
        }
        catch (Exception e) when (e is HttpRequestException or IOException or OperationCanceledException) { return DeliveryResult.Error("SMS_CONEXION", true); }
        catch (JsonException) { return DeliveryResult.Error("SMS_RESPUESTA_INVALIDA", true); }
    }
}
