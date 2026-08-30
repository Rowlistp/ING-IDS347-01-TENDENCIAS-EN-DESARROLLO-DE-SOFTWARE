using System.Security.Cryptography;

namespace FuelTrack.Api.Security;

public sealed class PasswordService
{
    private const string Algorithm = "PBKDF2-SHA512";
    private const int Iterations = 210_000;
    private const int MinimumAcceptedIterations = 100_000;
    private const int MaximumAcceptedIterations = 1_000_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    public const int MinimumLength = 12;

    public string Hash(string password)
    {
        ValidatePolicy(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return string.Join(
            '$',
            Algorithm,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm)
            return false;

        if (!int.TryParse(parts[1], out var iterations) ||
            iterations < MinimumAcceptedIterations ||
            iterations > MaximumAcceptedIterations)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            if (salt.Length != SaltSize || expected.Length != HashSize)
                return false;

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA512,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static void ValidatePolicy(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña es obligatoria.", nameof(password));

        if (password.Length < MinimumLength ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new ArgumentException(
                $"La contraseña debe tener al menos {MinimumLength} caracteres, mayúscula, minúscula, número y carácter especial.",
                nameof(password));
        }
    }
}
