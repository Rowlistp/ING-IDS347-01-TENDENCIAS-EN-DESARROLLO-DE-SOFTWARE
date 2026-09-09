using System.Collections.Concurrent;
using FuelTrack.Api.Notifications;
using FuelTrack.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Api.Tests.Notifications;

internal sealed class NotificationWebFactory(string connection, NotificationOptions options, TicketOptions ticketOptions) : WebApplicationFactory<Program>
{
    public CaptureLoggerProvider Logs { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["Jwt:Key"] = "TEST-JWT-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            ["Tickets:SigningPrivateKeyPkcs8Base64"] = ticketOptions.SigningPrivateKeyPkcs8Base64,
            ["Tickets:SigningPublicKeySpkiBase64"] = ticketOptions.SigningPublicKeySpkiBase64,
            ["Notifications:WorkerEnabled"] = "false",
            ["Notifications:AllowInsecureLocalTransport"] = "true",
            ["Notifications:Smtp:Enabled"] = "true", ["Notifications:Smtp:Host"] = "127.0.0.1",
            ["Notifications:Smtp:Port"] = options.Smtp.Port.ToString(), ["Notifications:Smtp:StartTls"] = "false",
            ["Notifications:Smtp:FromAddress"] = "fueltrack@example.test",
            ["Notifications:Sms:Enabled"] = "true", ["Notifications:Sms:BaseUrl"] = options.Sms.BaseUrl,
            ["Notifications:Sms:ApiKey"] = "fixture-key-only", ["Notifications:PublicBaseUrl"] = "https://localhost"
        }));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }
}
internal sealed class CaptureLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Lines { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Lines);
    public void Dispose() { }
    private sealed class CaptureLogger(ConcurrentQueue<string> lines) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => lines.Enqueue(formatter(state, exception));
    }
}
