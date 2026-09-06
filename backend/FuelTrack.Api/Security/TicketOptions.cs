using System.Text.RegularExpressions;

namespace FuelTrack.Api.Security;

public sealed partial class TicketOptions
{
    public const string SectionName = "Tickets";

    public string Prefix { get; set; } = "COM";
    public string SigningPrivateKeyPkcs8Base64 { get; set; } = string.Empty;
    public string SigningPublicKeySpkiBase64 { get; set; } = string.Empty;

    public string GetValidatedPrefix(string? requestedPrefix = null)
    {
        var value = (requestedPrefix ?? Prefix).Trim().ToUpperInvariant();
        if (!PrefixPattern().IsMatch(value))
            throw new InvalidOperationException("El prefijo de tickets debe tener entre 2 y 10 caracteres alfanuméricos.");

        return value;
    }

    [GeneratedRegex("^[A-Z0-9]{2,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex PrefixPattern();
}
