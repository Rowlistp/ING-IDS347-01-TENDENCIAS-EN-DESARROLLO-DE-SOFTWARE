using FuelTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<TipoCombustible> TiposCombustible => Set<TipoCombustible>();
    public DbSet<Estacion> Estaciones => Set<Estacion>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<SolicitudCombustible> SolicitudesCombustible => Set<SolicitudCombustible>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Tanque> Tanques => Set<Tanque>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<RecepcionCombustible> RecepcionesCombustible => Set<RecepcionCombustible>();
    public DbSet<Despacho> Despachos => Set<Despacho>();
    public DbSet<CierreDiario> CierresDiarios => Set<CierreDiario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsuarioRol>()
            .HasKey(ur => new { ur.UsuarioId, ur.RolId });

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Auditoria>()
            .Property(a => a.DatosRelevantes)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario).IsUnique();
        modelBuilder.Entity<Empleado>()
            .HasIndex(e => e.Codigo).IsUnique();
        modelBuilder.Entity<Empleado>()
            .HasIndex(e => e.Cedula).IsUnique();
        modelBuilder.Entity<Vehiculo>()
            .HasIndex(v => v.Placa).IsUnique();
        modelBuilder.Entity<Vehiculo>()
            .HasIndex(v => v.Ficha).IsUnique();
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.NumeroSecuencial).IsUnique();
        modelBuilder.Entity<Tanque>()
            .HasIndex(t => t.Identificacion).IsUnique();
        modelBuilder.Entity<Despacho>()
            .HasIndex(d => d.TicketId).IsUnique();
        modelBuilder.Entity<CierreDiario>()
            .HasIndex(c => c.Fecha).IsUnique();
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash).IsUnique();
        modelBuilder.Entity<TipoCombustible>()
            .HasIndex(t => t.Nombre).IsUnique();
        modelBuilder.Entity<Proveedor>()
            .HasIndex(p => p.Rnc).IsUnique();

        modelBuilder.Entity<Vehiculo>().Property(v => v.CapacidadTanque).HasPrecision(18, 4);
        modelBuilder.Entity<Vehiculo>().Property(v => v.Odometro).HasPrecision(18, 4);
        modelBuilder.Entity<SolicitudCombustible>().Property(s => s.CantidadSolicitada).HasPrecision(18, 4);
        modelBuilder.Entity<SolicitudCombustible>().Property(s => s.CantidadAutorizada).HasPrecision(18, 4);
        modelBuilder.Entity<Ticket>().Property(t => t.CantidadAutorizada).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.Capacidad).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.NivelActual).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.NivelCritico).HasPrecision(18, 4);
        modelBuilder.Entity<Inventario>().Property(i => i.ExistenciaActual).HasPrecision(18, 4);
        modelBuilder.Entity<Inventario>().Property(i => i.Disponibilidad).HasPrecision(18, 4);
        modelBuilder.Entity<MovimientoInventario>().Property(m => m.Volumen).HasPrecision(18, 4);
        modelBuilder.Entity<RecepcionCombustible>().Property(r => r.VolumenRecibido).HasPrecision(18, 4);
        modelBuilder.Entity<Despacho>().Property(d => d.GalonesServidos).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiario>().Property(c => c.VolumenDespachado).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiario>().Property(c => c.InventarioFinal).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiario>().Property(c => c.Diferencias).HasPrecision(18, 4);

        modelBuilder.Entity<Despacho>()
            .HasOne(d => d.Ticket)
            .WithOne(t => t.Despacho)
            .HasForeignKey<Despacho>(d => d.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Despacho>()
            .HasOne(d => d.Operador)
            .WithMany(u => u.DespachosOperados)
            .HasForeignKey(d => d.OperadorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Auditoria>()
            .HasOne(a => a.Usuario)
            .WithMany(u => u.Auditorias)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.Usuario)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}
