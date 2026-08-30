using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FuelTrack.Api.Models;
using FuelTrack.Api.Security;
using Microsoft.Extensions.Options;

namespace FuelTrack.Api.Tests.Security;

[TestClass]
public sealed class TokenServiceTests
{
    private static TokenService CreateService()
        => new(Options.Create(new JwtOptions
        {
            Issuer = "FuelTrack.Tests",
            Audience = "FuelTrack.TestClients",
            Key = "TEST-ONLY-KEY-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));

    [TestMethod]
    public void CreateAccessToken_IncludesIdentityAndRoles()
    {
        var sut = CreateService();
        var user = new Usuario
        {
            Id = 42,
            NombreUsuario = "admin",
            Activo = true
        };

        var result = sut.CreateAccessToken(
            user,
            [Roles.Administrador, Roles.Auditor]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.AreEqual("42", jwt.Subject);
        Assert.AreEqual("admin", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);

        var roles = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        CollectionAssert.Contains(roles, Roles.Administrador);
        CollectionAssert.Contains(roles, Roles.Auditor);
        Assert.IsTrue(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [TestMethod]
    public void CreateRefreshToken_GeneratesDistinctRandomValues()
    {
        var sut = CreateService();

        var first = sut.CreateRefreshToken();
        var second = sut.CreateRefreshToken();

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(first.Length >= 64);
        Assert.IsTrue(second.Length >= 64);
    }

    [TestMethod]
    public void HashRefreshToken_IsDeterministicAndDoesNotExposeRawToken()
    {
        const string raw = "refresh-token-de-prueba";

        var first = TokenService.HashRefreshToken(raw);
        var second = TokenService.HashRefreshToken(raw);

        Assert.AreEqual(first, second);
        Assert.AreNotEqual(raw, first);
        Assert.AreEqual(64, first.Length);
    }
}
