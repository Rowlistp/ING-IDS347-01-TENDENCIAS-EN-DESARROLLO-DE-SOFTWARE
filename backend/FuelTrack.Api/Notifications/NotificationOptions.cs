using System.Text.RegularExpressions;
using MimeKit;

namespace FuelTrack.Api.Notifications;

public sealed class NotificationOptions
{
    public bool WorkerEnabled { get; set; }
    public int BatchSize { get; set; } = 10;
    public int PollIntervalSeconds { get; set; } = 10;
    public int RuleIntervalSeconds { get; set; } = 60;
    public int LockSeconds { get; set; } = 120;
    public int TransportTimeoutSeconds { get; set; } = 30;
    public int MaxAttempts { get; set; } = 4;
    public int BaseRetrySeconds { get; set; } = 60;
    public int MaxRetrySeconds { get; set; } = 3600;
    public int TicketExpiringSoonHours { get; set; } = 24;
    public int TicketLinkHours { get; set; } = 48;
    public int LowInventoryPeriodHours { get; set; } = 24;
    public string PublicBaseUrl { get; set; } = "";
    public bool AllowInsecureLocalTransport { get; set; }
    public SmtpOptions Smtp { get; set; } = new();
    public SmsOptions Sms { get; set; } = new();
    public OperationsOptions Operations { get; set; } = new();
    public bool IsValid(bool localEnvironment)
    {
        bool Url(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
            (uri.Scheme == "https" || (localEnvironment && AllowInsecureLocalTransport && uri.Scheme == "http" && uri.IsLoopback));
        return BatchSize is >= 1 and <= 50 && PollIntervalSeconds is >= 1 and <= 3600 && RuleIntervalSeconds is >= 1 and <= 86400 &&
            TransportTimeoutSeconds is >= 1 and <= 120 && LockSeconds >= 2 * TransportTimeoutSeconds + 10 && LockSeconds <= 3600 &&
            MaxAttempts is >= 1 and <= 10 && BaseRetrySeconds >= 1 && MaxRetrySeconds >= BaseRetrySeconds && MaxRetrySeconds <= 86400 &&
            TicketExpiringSoonHours is >= 1 and <= 720 && TicketLinkHours is >= 1 and <= 720 && LowInventoryPeriodHours is >= 1 and <= 720 &&
            (!AllowInsecureLocalTransport || localEnvironment) &&
            (!Smtp.Enabled || (Smtp.Host.Length > 0 && Smtp.Port is > 0 and <= 65535 && ValidEmail(Smtp.FromAddress) &&
                !(Smtp.UseSsl && Smtp.StartTls) && ((Smtp.UseSsl || Smtp.StartTls) || (localEnvironment && AllowInsecureLocalTransport && Uri.CheckHostName(Smtp.Host) != UriHostNameType.Unknown && new[] { "localhost", "127.0.0.1", "::1" }.Contains(Smtp.Host))) &&
                (Smtp.Username.Length == 0 || Smtp.Password.Length > 0))) &&
            (!Sms.Enabled || (Url(Sms.BaseUrl) && Url(PublicBaseUrl) && Sms.ApiKey.Length > 0 &&
                Regex.IsMatch(Sms.AuthHeaderName, "^[A-Za-z][A-Za-z0-9-]{0,63}$") &&
                !new[] { "host", "content-length", "content-type", "idempotency-key" }.Contains(Sms.AuthHeaderName.ToLowerInvariant()) &&
                !Sms.ApiKey.Contains('\r') && !Sms.ApiKey.Contains('\n'))) &&
            Operations.Emails.All(ValidEmail) && Operations.Phones.All(ValidPhone);
    }
    public static bool ValidEmail(string value) => value.Length <= 254 && !value.Contains('\r') && !value.Contains('\n') &&
        MailboxAddress.TryParse(value, out var address) && address.Address == value && value.Contains('@');
    public static bool ValidPhone(string value) => Regex.IsMatch(value, @"^\+[1-9][0-9]{7,14}$");
    public int RetrySeconds(int attempts) => (int)Math.Min(MaxRetrySeconds, BaseRetrySeconds * Math.Pow(2, Math.Min(attempts - 1, 20)));
}
public sealed class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; }
    public bool StartTls { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "FuelTrack";
}
public sealed class SmsOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "";
    public string Sender { get; set; } = "FuelTrack";
    public string AuthHeaderName { get; set; } = "Authorization";
    public string ApiKey { get; set; } = "";
}
public sealed class OperationsOptions
{
    public string[] Emails { get; set; } = [];
    public string[] Phones { get; set; } = [];
}
