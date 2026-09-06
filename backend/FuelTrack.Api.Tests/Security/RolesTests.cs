using FuelTrack.Api.Security;

namespace FuelTrack.Api.Tests.Security;

[TestClass]
public sealed class RolesTests
{
    [TestMethod]
    public void Todos_ContainsExpectedUniqueRoles()
    {
        var expected = new[]
        {
            "Administrador",
            "Supervisor",
            "Despachador",
            "Auditor",
            "Consulta",
            "Solicitante"
        };

        CollectionAssert.AreEquivalent(expected, Roles.Todos);
        Assert.AreEqual(Roles.Todos.Length, Roles.Todos.Distinct().Count());
    }
}
