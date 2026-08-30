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

    [DataTestMethod]
    [DataRow("")]
    [DataRow("texto-no-valido")]
    [DataRow("PBKDF2-SHA512$abc$xxx$yyy")]
    public void Verify_WithMalformedHash_ReturnsFalse(string storedHash)
    {
        Assert.IsFalse(_sut.Verify("Clave-123!", storedHash));
    }
}
