using System.Text.Json;
using FuelTrack.Api.Notifications;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Tests.Notifications;

[TestClass]
public sealed class TextbeeTransportTests
{
    private const string Device = "0123456789abcdef01234567";
    private static NotificationOptions Config(LocalGateway gateway) => new()
    {
        PublicBaseUrl = "https://tickets.example.test", AllowInsecureLocalTransport = true,
        Sms = new() { Enabled = true, Provider = "Textbee", DeviceId = Device, BaseUrl = gateway.Url, ApiKey = "fixture-only" }
    };

    [TestMethod]
    public async Task SendsTextbeeContractAndPreservesBatchId()
    {
        await using var gateway = new LocalGateway { AuthHeaderName = "x-api-key", Response = "{\"data\":{\"success\":true,\"recipientCount\":1,\"smsBatchId\":\"batch-123\"}}" };
        using var client = new HttpClient();
        var result = await new HttpSmsGatewaySender(client, Options.Create(Config(gateway))).SendAsync(new(1, "ticket-1", "8095550101", "Ticket", "Texto de prueba"), default);
        Assert.IsTrue(result.Success); Assert.AreEqual("batch-123", result.ProviderId);
        var request = gateway.Requests.Single();
        Assert.AreEqual("fixture-only", request.Auth);
        using var body = JsonDocument.Parse(request.Body);
        Assert.AreEqual("+18095550101", body.RootElement.GetProperty("recipients")[0].GetString());
        Assert.AreEqual(Device, body.RootElement.GetProperty("deviceId").GetString());
        Assert.AreEqual("Texto de prueba", body.RootElement.GetProperty("message").GetString());
        Assert.IsFalse(body.RootElement.TryGetProperty("to", out _));
    }

    [TestMethod]
    [DataRow("{\"data\":{\"successCount\":1,\"failureCount\":0}}", true)]
    [DataRow("{\"data\":{\"successCount\":0,\"failureCount\":1}}", false)]
    [DataRow("{\"data\":{\"success\":false,\"recipientCount\":1,\"smsBatchId\":\"id\"}}", false)]
    [DataRow("{\"data\":{\"success\":true,\"recipientCount\":\"1\",\"smsBatchId\":\"id\"}}", false)]
    [DataRow("{\"data\":{\"success\":true}}", false)]
    [DataRow("{\"messageId\":\"generic-id\"}", false)]
    [DataRow("[]", false)] [DataRow("null", false)] [DataRow("invalid", false)]
    public async Task AcknowledgementMustBeUnambiguous(string body, bool expected)
    {
        await using var gateway = new LocalGateway { Response = body };
        using var client = new HttpClient();
        var result = await new HttpSmsGatewaySender(client, Options.Create(Config(gateway))).SendAsync(new(1, "key", "+18095550101", "T", "T"), default);
        Assert.AreEqual(expected, result.Success);
        Assert.IsFalse(result.Transient, "No repetir automáticamente un SMS cuyo resultado es ambiguo.");
    }

    [TestMethod]
    [DataRow(401, false)] [DataRow(400, false)] [DataRow(429, true)] [DataRow(500, false)]
    public async Task RejectionsAndAmbiguousErrorsDoNotAssumeDelivery(int status, bool retry)
    {
        await using var gateway = new LocalGateway { Status = status };
        using var client = new HttpClient();
        var result = await new HttpSmsGatewaySender(client, Options.Create(Config(gateway))).SendAsync(new(1, "key", "+18095550101", "T", "T"), default);
        Assert.IsFalse(result.Success); Assert.AreEqual(retry, result.Transient);
    }

    [TestMethod]
    public async Task ConfigRequiresExplicitDeviceAndKnownProvider()
    {
        await using var gateway = new LocalGateway();
        var options = Config(gateway);
        Assert.IsTrue(options.IsValid(true));
        options.Sms.DeviceId = ""; Assert.IsFalse(options.IsValid(true));
        options.Sms.DeviceId = Device; options.Sms.Provider = "typo"; Assert.IsFalse(options.IsValid(true));
    }
}
