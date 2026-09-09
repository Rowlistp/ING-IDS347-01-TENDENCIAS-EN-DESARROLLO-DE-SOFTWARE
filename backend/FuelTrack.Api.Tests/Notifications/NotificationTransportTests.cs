using System.Text.Json;
using FuelTrack.Api.Notifications;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FuelTrack.Api.Tests.Notifications;

[TestClass]
public sealed class NotificationTransportTests
{
    private static NotificationOptions OptionsFor(LocalGateway gateway) => new()
    {
        Sms = new() { Enabled = true, BaseUrl = gateway.Url, ApiKey = "fixture-key-only" },
        Smtp = new() { Enabled = true, Host = "127.0.0.1", FromAddress = "test@example.test", StartTls = false }
    };
    [TestMethod]
    public async Task Sms_RealHttpPayloadAndHeaders()
    {
        await using var gateway = new LocalGateway(); using var client = new HttpClient();
        var sender = new HttpSmsGatewaySender(client, Options.Create(OptionsFor(gateway)));
        var message = new OutgoingNotification(1, "TICKET:1", "+18095550101", "Ticket", "Ticket COM-2026-000001 https://example.test/download/fixture");
        Assert.IsTrue((await sender.SendAsync(message, default)).Success);
        var request = gateway.Requests.Single(); using var json = JsonDocument.Parse(request.Body);
        Assert.AreEqual(message.To, json.RootElement.GetProperty("to").GetString());
        Assert.AreEqual(message.Text, json.RootElement.GetProperty("message").GetString());
        Assert.AreEqual(message.Key, json.RootElement.GetProperty("reference").GetString());
        Assert.AreEqual("fixture-key-only", request.Auth); Assert.AreEqual(64, request.Idempotency!.Length);
    }
    [TestMethod]
    [DataRow(429, true)] [DataRow(500, true)] [DataRow(400, false)] [DataRow(401, false)] [DataRow(403, false)]
    public async Task Sms_ClassifiesHttpStatus(int status, bool transient)
    {
        await using var gateway = new LocalGateway { Status = status, Response = "secret-provider-body" }; using var client = new HttpClient();
        var result = await new HttpSmsGatewaySender(client, Options.Create(OptionsFor(gateway))).SendAsync(new(1, "test", "+18095550101", "test", "test"), default);
        Assert.IsFalse(result.Success); Assert.AreEqual(transient, result.Transient); Assert.AreEqual($"SMS_HTTP_{status}", result.Code);
    }
    [TestMethod]
    [DataRow("garbage")] [DataRow("{}") ] [DataRow("{\"messageId\":\"https://secret.example/token\"}")]
    public async Task Sms_InvalidAcknowledgementIsRetryable(string response)
    {
        await using var gateway = new LocalGateway { Response = response }; using var client = new HttpClient();
        var result = await new HttpSmsGatewaySender(client, Options.Create(OptionsFor(gateway))).SendAsync(new(1, "test", "+18095550101", "test", "test"), default);
        Assert.AreEqual("SMS_RESPUESTA_INVALIDA", result.Code); Assert.IsTrue(result.Transient);
    }
    [TestMethod]
    public async Task Sms_TimeoutAndRefusedConnectionAreRetryable()
    {
        var gateway = new LocalGateway { DelayMs = 200 }; var options = Options.Create(OptionsFor(gateway));
        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(50) };
        var sender = new HttpSmsGatewaySender(client, options);
        Assert.IsTrue((await sender.SendAsync(new(1, "test", "+18095550101", "test", "test"), default)).Transient);
        await gateway.DisposeAsync();
        Assert.IsTrue((await sender.SendAsync(new(1, "test", "+18095550101", "test", "test"), default)).Transient);
    }
    [TestMethod]
    public void Smtp_MimeContainsPdfAndDeterministicMessageId()
    {
        var sender = new SmtpEmailSender(Options.Create(new NotificationOptions { Smtp = new() { FromAddress = "test@example.test" } }));
        var input = new OutgoingNotification(4, "key", "employee@example.test", "Ticket COM-1", "Body", "%PDF-test"u8.ToArray(), "ticket.pdf");
        using var first = sender.BuildMessage(input); using var next = sender.BuildMessage(input);
        Assert.AreEqual(first.MessageId, next.MessageId); Assert.AreEqual(input.Text, first.TextBody);
        Assert.AreEqual(input.To, first.To.Mailboxes.Single().Address);
        var pdf = (MimePart)first.Attachments.Single(); Assert.AreEqual("application/pdf", pdf.ContentType.MimeType);
        Assert.AreEqual("ticket.pdf", pdf.FileName);
    }
    [TestMethod]
    [DataRow(451, true)] [DataRow(550, false)]
    public async Task Smtp_RealTcpFailuresAreSanitized(int status, bool transient)
    {
        await using var server = new RejectingSmtpServer(status);
        var options = Options.Create(new NotificationOptions { Smtp = new() { Enabled = true, Host = "127.0.0.1", Port = server.Port, StartTls = false, FromAddress = "test@example.test" } });
        var result = await new SmtpEmailSender(options).SendAsync(new(1, "test", "employee@example.test", "test", "test"), default);
        Assert.IsFalse(result.Success); Assert.AreEqual(transient, result.Transient); Assert.AreEqual($"SMTP_{status}", result.Code);
    }
    [TestMethod]
    public async Task InvalidRecipientsArePermanentFailures()
    {
        var options = Options.Create(new NotificationOptions()); using var client = new HttpClient();
        Assert.IsFalse((await new SmtpEmailSender(options).SendAsync(new(1, "key", "bad", "test", "test"), default)).Transient);
        Assert.IsFalse((await new HttpSmsGatewaySender(client, options).SendAsync(new(1, "key", "bad", "test", "test"), default)).Transient);
    }
    [TestMethod]
    public void ConfigAndBackoffAreBoundedAndSecretsOptionalWhenDisabled()
    {
        var o = new NotificationOptions(); Assert.IsTrue(o.IsValid(false));
        Assert.AreEqual(60, o.RetrySeconds(1)); Assert.AreEqual(3600, o.RetrySeconds(10));
        o.Sms.Enabled = true; Assert.IsFalse(o.IsValid(false));
        o.Sms.Enabled = false; o.LockSeconds = 5; Assert.IsFalse(o.IsValid(false));
        o.LockSeconds = 120; o.AllowInsecureLocalTransport = true; Assert.IsFalse(o.IsValid(false));
    }
}
