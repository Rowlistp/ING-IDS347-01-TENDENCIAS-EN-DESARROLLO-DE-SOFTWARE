using FuelTrack.Api.Data;
using FuelTrack.Api.Models;
using FuelTrack.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Tests.Services;

[TestClass]
public sealed class AuditServiceTests
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private AuditService _service = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        _service = new AuditService(_db);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_db is not null) await _db.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }

    [TestMethod]
    public async Task GetPageAsync_ResuelveNombreUsuario_CuandoUsuarioExiste()
    {
        var usuario = new Usuario { NombreUsuario = "jperez", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        _db.Auditorias.Add(new Auditoria
        {
            Evento = "LOGIN",
            EntidadAfectada = "Usuario",
            IdentificadorRegistro = usuario.Id.ToString(),
            FechaHora = DateTime.UtcNow,
            UsuarioId = usuario.Id
        });
        await _db.SaveChangesAsync();

        var page = await _service.GetPageAsync(1, 50, CancellationToken.None);

        Assert.AreEqual(1, page.Elementos.Count);
        Assert.AreEqual("jperez", page.Elementos.First().NombreUsuario);
    }

    [TestMethod]
    public async Task GetPageAsync_NombreUsuarioEsNull_CuandoUsuarioIdEsNull()
    {
        _db.Auditorias.Add(new Auditoria
        {
            Evento = "LOGIN_FALLIDO",
            EntidadAfectada = "Usuario",
            IdentificadorRegistro = "desconocido",
            FechaHora = DateTime.UtcNow,
            UsuarioId = null
        });
        await _db.SaveChangesAsync();

        var page = await _service.GetPageAsync(1, 50, CancellationToken.None);

        Assert.AreEqual(1, page.Elementos.Count);
        Assert.IsNull(page.Elementos.First().UsuarioId);
        Assert.IsNull(page.Elementos.First().NombreUsuario);
    }

    [TestMethod]
    public async Task GetPageAsync_NombreUsuarioEsNull_CuandoUsuarioFueBorrado()
    {
        // UsuarioId apunta a un usuario que ya no existe en la tabla Usuarios.
        // No debe lanzar excepción: debe comportarse como left join, no inner join.
        // Se desactiva temporalmente el enforcement de FK de SQLite para simular
        // un UsuarioId huérfano (en Postgres el FK real usa OnDelete SetNull,
        // pero este caso cubre datos legados/inconsistentes).
        await _db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
        _db.Auditorias.Add(new Auditoria
        {
            Evento = "ELIMINAR_DESPACHO",
            EntidadAfectada = "Despacho",
            IdentificadorRegistro = "123",
            FechaHora = DateTime.UtcNow,
            UsuarioId = 999
        });
        await _db.SaveChangesAsync();

        var page = await _service.GetPageAsync(1, 50, CancellationToken.None);

        Assert.AreEqual(1, page.Elementos.Count);
        Assert.AreEqual(999, page.Elementos.First().UsuarioId);
        Assert.IsNull(page.Elementos.First().NombreUsuario);
    }

    [TestMethod]
    public async Task GetPageAsync_PaginacionSigueFuncionandoIgualQueAntes()
    {
        var usuario = new Usuario { NombreUsuario = "admin", PasswordHash = "hash", Activo = true };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
        {
            _db.Auditorias.Add(new Auditoria
            {
                Evento = $"EVENTO_{i}",
                EntidadAfectada = "Test",
                IdentificadorRegistro = i.ToString(),
                FechaHora = DateTime.UtcNow.AddMinutes(i),
                UsuarioId = usuario.Id
            });
        }
        await _db.SaveChangesAsync();

        var page1 = await _service.GetPageAsync(1, 2, CancellationToken.None);
        Assert.AreEqual(1, page1.Pagina);
        Assert.AreEqual(2, page1.TamanoPagina);
        Assert.AreEqual(5, page1.Total);
        Assert.AreEqual(2, page1.Elementos.Count);
        // orden descendente por FechaHora: el más reciente primero
        Assert.AreEqual("EVENTO_4", page1.Elementos.First().Evento);

        var page2 = await _service.GetPageAsync(2, 2, CancellationToken.None);
        Assert.AreEqual(2, page2.Elementos.Count);
        Assert.AreEqual("EVENTO_2", page2.Elementos.First().Evento);

        var page3 = await _service.GetPageAsync(3, 2, CancellationToken.None);
        Assert.AreEqual(1, page3.Elementos.Count);
        Assert.AreEqual("EVENTO_0", page3.Elementos.First().Evento);
    }
}
