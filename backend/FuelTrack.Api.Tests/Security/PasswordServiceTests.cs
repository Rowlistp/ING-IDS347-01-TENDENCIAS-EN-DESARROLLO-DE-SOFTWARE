using FuelTrack.Api.Security;

namespace FuelTrack.Api.Tests.Security;

[TestClass]
public sealed class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [TestMethod]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "Clave-Fuerte-123!";

        var hash = _sut.Hash(password);

        Assert.IsTrue(_sut.Verify(password, hash));
        Assert.AreNotEqual(password, hash);
    }

    [TestMethod]
    public void Hash_SamePasswordTwice_UsesDifferentSalt()
    {
        const string password = "Clave-Fuerte-123!";

        var first = _sut.Hash(password);
        var second = _sut.Hash(password);

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(_sut.Verify(password, first));
        Assert.IsTrue(_sut.Verify(password, second));
    }

    [TestMethod]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("Correcta-123!");

        Assert.IsFalse(_sut.Verify("Incorrecta-123!", hash));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("texto-no-valido")]
    [DataRow("PBKDF2-SHA512$abc$xxx$yyy")]
    [DataRow("PBKDF2-SHA256$210000$MDEyMzQ1Njc4OWFiY2RlZg==$MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=")]
    [DataRow("PBKDF2-SHA512$99999$MDEyMzQ1Njc4OWFiY2RlZg==$MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=")]
    [DataRow("PBKDF2-SHA512$1000001$MDEyMzQ1Njc4OWFiY2RlZg==$MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=")]
    [DataRow("PBKDF2-SHA512$210000$Y29ydGE=$MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=")]
    [DataRow("PBKDF2-SHA512$210000$MDEyMzQ1Njc4OWFiY2RlZg==$Y29ydG8=")]
    public void Verify_WithMalformedHash_ReturnsFalse(string storedHash)
    {
        Assert.IsFalse(_sut.Verify("Clave-123!", storedHash));
    }

    [TestMethod]
    [DataRow("Corta-1!")]
    [DataRow("sin-mayuscula-1!")]
    [DataRow("SIN-MINUSCULA-1!")]
    [DataRow("SinNumero-Especial!")]
    [DataRow("SinEspecial123")]
    public void Hash_WithWeakPassword_RejectsPolicyViolation(string password)
    {
        Assert.ThrowsExactly<ArgumentException>(() => _sut.Hash(password));
    }

    [TestMethod]
    public void Hash_OverMaximumLength_RejectsPolicyViolation()
    {
        var password = $"Aa1!{new string('x', PasswordService.MaximumLength)}";

        Assert.ThrowsExactly<ArgumentException>(() => _sut.Hash(password));
    }

    [TestMethod]
    public void Verify_OversizedInputs_ReturnsFalseWithoutHashing()
    {
        var oversizedPassword = new string('x', PasswordService.MaximumLength + 1);
        var oversizedHash = new string('x', 513);

        Assert.IsFalse(_sut.Verify(oversizedPassword, "stored"));
        Assert.IsFalse(_sut.Verify("Clave-Fuerte-123!", oversizedHash));
    }
}
