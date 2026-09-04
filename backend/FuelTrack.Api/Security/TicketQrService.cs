using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using QRCoder;

namespace FuelTrack.Api.Security;

public sealed record TicketQrData(
    Guid TicketId,
    int NumeroSecuencial,
    string Prefijo,
    int SolicitudId,
    int EmpleadoId,
    int VehiculoId,
    int DepartamentoId,
    int TipoCombustibleId,
    decimal Cantidad,
    DateTime FechaEmisionUtc,
    DateTime FechaExpiracionUtc,
    string Token);

public sealed record GeneratedTicketQr(
    string Payload,
    string PayloadHash,
    string TokenHash,
    string Signature,
    byte[] Png,
    TicketQrData Data);

public sealed class TicketQrService(IOptions<TicketOptions> configuredOptions)
{
    private const string EnvelopeVersion = "FTQR1";
    private readonly TicketOptions _options = configuredOptions.Value;

    public GeneratedTicketQr Generate(
        Guid ticketId,
        int numeroSecuencial,
        string prefijo,
        int solicitudId,
        int empleadoId,
        int vehiculoId,
        int departamentoId,
        int tipoCombustibleId,
        decimal cantidad,
        DateTime fechaEmisionUtc,
        DateTime fechaExpiracionUtc)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var data = new TicketQrData(
            ticketId,
            numeroSecuencial,
            prefijo,
            solicitudId,
            empleadoId,
            vehiculoId,
            departamentoId,
            tipoCombustibleId,
            cantidad,
            fechaEmisionUtc,
            fechaExpiracionUtc,
            token);
        var canonicalBytes = Encoding.UTF8.GetBytes(ToCanonicalPayload(data));
        var hashBytes = SHA256.HashData(canonicalBytes);

        using var privateKey = LoadPrivateKey();
        var signatureBytes = privateKey.SignData(
            canonicalBytes,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        var hash = Convert.ToHexString(hashBytes);
        var signature = WebEncoders.Base64UrlEncode(signatureBytes);
        var payload = string.Join(
            '.',
            EnvelopeVersion,
            WebEncoders.Base64UrlEncode(canonicalBytes),
            hash,
            signature);

        using var qrData = QRCodeGenerator.GenerateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var png = qrCode.GetGraphic(8);

        return new GeneratedTicketQr(
            payload,
            hash,
            HashToken(token),
            signature,
            png,
            data);
    }

    public bool TryValidate(string payload, out TicketQrData? data, out string hash, out string signature)
    {
        data = null;
        hash = string.Empty;
        signature = string.Empty;

        if (string.IsNullOrWhiteSpace(payload) || payload.Length > 8192)
            return false;

        try
        {
            var parts = payload.Split('.', StringSplitOptions.None);
            if (parts.Length != 4 || !string.Equals(parts[0], EnvelopeVersion, StringComparison.Ordinal))
                return false;

            var canonicalBytes = WebEncoders.Base64UrlDecode(parts[1]);
            var expectedHash = Convert.FromHexString(parts[2]);
            if (expectedHash.Length != 32 ||
                !CryptographicOperations.FixedTimeEquals(SHA256.HashData(canonicalBytes), expectedHash))
            {
                return false;
            }

            var signatureBytes = WebEncoders.Base64UrlDecode(parts[3]);
            if (signatureBytes.Length != 64)
                return false;

            using var publicKey = LoadPublicKey();
            if (!publicKey.VerifyData(
                    canonicalBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                return false;
            }

            if (!TryParseCanonicalPayload(Encoding.UTF8.GetString(canonicalBytes), out data))
                return false;

            hash = parts[2].ToUpperInvariant();
            signature = parts[3];
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException or CryptographicException or OverflowException)
        {
            return false;
        }
    }

    public static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string ToCanonicalPayload(TicketQrData data)
        => string.Join('\n',
            "v=1",
            $"ticketId={data.TicketId:D}",
            $"numero={data.NumeroSecuencial.ToString(CultureInfo.InvariantCulture)}",
            $"prefijo={data.Prefijo}",
            $"solicitudId={data.SolicitudId.ToString(CultureInfo.InvariantCulture)}",
            $"empleadoId={data.EmpleadoId.ToString(CultureInfo.InvariantCulture)}",
            $"vehiculoId={data.VehiculoId.ToString(CultureInfo.InvariantCulture)}",
            $"departamentoId={data.DepartamentoId.ToString(CultureInfo.InvariantCulture)}",
            $"combustibleId={data.TipoCombustibleId.ToString(CultureInfo.InvariantCulture)}",
            $"cantidad={data.Cantidad.ToString("0.####", CultureInfo.InvariantCulture)}",
            $"emision={data.FechaEmisionUtc.ToUniversalTime():O}",
            $"expiracion={data.FechaExpiracionUtc.ToUniversalTime():O}",
            $"token={data.Token}");

    private static bool TryParseCanonicalPayload(string canonical, out TicketQrData? data)
    {
        data = null;
        var lines = canonical.Split('\n', StringSplitOptions.None);
        if (lines.Length != 13 || lines[0] != "v=1")
            return false;

        var expectedNames = new[]
        {
            "ticketId", "numero", "prefijo", "solicitudId", "empleadoId", "vehiculoId",
            "departamentoId", "combustibleId", "cantidad", "emision", "expiracion", "token"
        };
        var values = new string[expectedNames.Length];
        for (var index = 0; index < expectedNames.Length; index++)
        {
            var expectedPrefix = expectedNames[index] + "=";
            if (!lines[index + 1].StartsWith(expectedPrefix, StringComparison.Ordinal))
                return false;
            values[index] = lines[index + 1][expectedPrefix.Length..];
        }

        if (!Guid.TryParseExact(values[0], "D", out var ticketId) ||
            !int.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out var numero) || numero <= 0 ||
            string.IsNullOrWhiteSpace(values[2]) || values[2].Length > 10 ||
            !int.TryParse(values[3], NumberStyles.None, CultureInfo.InvariantCulture, out var solicitudId) || solicitudId <= 0 ||
            !int.TryParse(values[4], NumberStyles.None, CultureInfo.InvariantCulture, out var empleadoId) || empleadoId <= 0 ||
            !int.TryParse(values[5], NumberStyles.None, CultureInfo.InvariantCulture, out var vehiculoId) || vehiculoId <= 0 ||
            !int.TryParse(values[6], NumberStyles.None, CultureInfo.InvariantCulture, out var departamentoId) || departamentoId <= 0 ||
            !int.TryParse(values[7], NumberStyles.None, CultureInfo.InvariantCulture, out var combustibleId) || combustibleId <= 0 ||
            !decimal.TryParse(values[8], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var cantidad) || cantidad <= 0 ||
            !DateTimeOffset.TryParseExact(values[9], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var emision) ||
            !DateTimeOffset.TryParseExact(values[10], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiracion))
        {
            return false;
        }

        var tokenBytes = WebEncoders.Base64UrlDecode(values[11]);
        if (tokenBytes.Length != 32)
            return false;

        data = new TicketQrData(
            ticketId,
            numero,
            values[2],
            solicitudId,
            empleadoId,
            vehiculoId,
            departamentoId,
            combustibleId,
            cantidad,
            emision.UtcDateTime,
            expiracion.UtcDateTime,
            values[11]);
        return true;
    }

    private ECDsa LoadPrivateKey()
    {
        if (string.IsNullOrWhiteSpace(_options.SigningPrivateKeyPkcs8Base64))
            throw new InvalidOperationException("Falta Tickets:SigningPrivateKeyPkcs8Base64. Configure la clave ECDSA P-256 fuera de Git.");

        var key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(Convert.FromBase64String(_options.SigningPrivateKeyPkcs8Base64), out _);
        EnsureP256(key);
        return key;
    }

    private ECDsa LoadPublicKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.SigningPublicKeySpkiBase64))
        {
            var publicKey = ECDsa.Create();
            publicKey.ImportSubjectPublicKeyInfo(
                Convert.FromBase64String(_options.SigningPublicKeySpkiBase64),
                out _);
            EnsureP256(publicKey);
            return publicKey;
        }

        using var privateKey = LoadPrivateKey();
        var derived = ECDsa.Create();
        derived.ImportSubjectPublicKeyInfo(privateKey.ExportSubjectPublicKeyInfo(), out _);
        return derived;
    }

    private static void EnsureP256(ECDsa key)
    {
        if (key.KeySize != 256)
        {
            key.Dispose();
            throw new CryptographicException("La clave de tickets debe usar la curva ECDSA P-256.");
        }
    }
}
