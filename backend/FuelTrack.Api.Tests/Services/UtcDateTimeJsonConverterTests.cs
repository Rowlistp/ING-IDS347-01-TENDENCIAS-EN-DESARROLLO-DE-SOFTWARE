using System.Text.Json;
using FuelTrack.Api.Services;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class UtcDateTimeJsonConverterTests
{
    [TestMethod]
    [DataRow("2026-09-25T16:30:00Z")]
    [DataRow("2026-09-25T16:30:00+00:00")]
    [DataRow("2026-09-25T12:30:00-04:00")]
    [DataRow("2026-09-25T22:00:00+05:30")]
    [DataRow("2026-09-25T16:30:00")]
    public void EquivalentInputDatesPreserveTheirInstantAndUseUtc(string input)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());
        var value = JsonSerializer.Deserialize<DateTime>(JsonSerializer.Serialize(input), options);
        Assert.AreEqual(DateTimeKind.Utc, value.Kind);
        Assert.AreEqual(new DateTime(2026, 9, 25, 16, 30, 0, DateTimeKind.Utc), value);
        Assert.AreEqual("\"2026-09-25T16:30:00Z\"", JsonSerializer.Serialize(value, options));
    }
}
