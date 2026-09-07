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
    public DbSet<CierreDiarioDetalle> CierresDiariosDetalle => Set<CierreDiarioDetalle>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<TicketDeliveryLink> TicketDeliveryLinks => Set<TicketDeliveryLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacion>().Property(n => n.ClaveIdempotencia).HasMaxLength(1000);
        modelBuilder.Entity<Notificacion>().Property(n => n.UltimoError).HasMaxLength(300);
        modelBuilder.Entity<Notificacion>().Property(n => n.ProveedorMensajeId).HasMaxLength(200);
        modelBuilder.Entity<Notificacion>().Property(n => n.Mensaje).HasMaxLength(2000);
        modelBuilder.Entity<Notificacion>().HasIndex(n => n.ClaveIdempotencia).IsUnique();
        modelBuilder.Entity<Notificacion>().HasIndex(n => new { n.ProximoIntentoUtc, n.Id }).HasFilter("\"Estado\" = 'PENDIENTE'");
        modelBuilder.Entity<Notificacion>().HasIndex(n => n.BloqueadaHastaUtc).HasFilter("\"Estado\" = 'PROCESANDO'");
        modelBuilder.Entity<Notificacion>().HasIndex(n => new { n.Tipo, n.ReferenciaEvento });
        modelBuilder.Entity<TicketDeliveryLink>().Property(l => l.TokenHash).HasMaxLength(64);
        modelBuilder.Entity<TicketDeliveryLink>().HasIndex(l => l.TokenHash).IsUnique();
        modelBuilder.Entity<TicketDeliveryLink>().HasOne(l => l.Ticket).WithMany().HasForeignKey(l => l.TicketId).OnDelete(DeleteBehavior.Restrict);
        if (Database.IsNpgsql())
        {
            modelBuilder.HasSequence<long>("ticket_numero_seq");
            modelBuilder.Entity<Inventario>().Property<uint>("xmin").IsRowVersion();
            modelBuilder.Entity<Ticket>().Property<uint>("xmin").IsRowVersion();
        }

        modelBuilder.Entity<UsuarioRol>()
            .HasKey(ur => new { ur.UsuarioId, ur.RolId });

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Ticket>()
            .Property(t => t.QrCodePng)
            .HasColumnType("bytea");

        modelBuilder.Entity<Auditoria>()
            .Property(a => a.DatosRelevantes)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario).IsUnique();
        modelBuilder.Entity<Usuario>()
            .Property(u => u.SecurityVersion).HasDefaultValue(1);
        modelBuilder.Entity<Rol>()
            .HasIndex(r => r.Nombre).IsUnique();
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
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.SolicitudId)
            .IsUnique()
            .HasDatabaseName("UX_Tickets_Solicitud_Utilizable")
            .HasFilter("\"SolicitudId\" IS NOT NULL AND \"Estado\" NOT IN (4, 5, 6)");
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
        modelBuilder.Entity<SolicitudCombustible>()
            .Property(s => s.Estado)
            .HasConversion<string>();
        modelBuilder.Entity<Ticket>().Property(t => t.CantidadAutorizada).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.Capacidad).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.NivelActual).HasPrecision(18, 4);
        modelBuilder.Entity<Tanque>().Property(t => t.NivelCritico).HasPrecision(18, 4);
        modelBuilder.Entity<Inventario>().Property(i => i.ExistenciaActual).HasPrecision(18, 4);
        modelBuilder.Entity<Inventario>().Property(i => i.Disponibilidad).HasPrecision(18, 4);
        modelBuilder.Entity<MovimientoInventario>().Property(m => m.Volumen).HasPrecision(18, 4);
        modelBuilder.Entity<RecepcionCombustible>().Property(r => r.VolumenRecibido).HasPrecision(18, 4);
        modelBuilder.Entity<Despacho>().Property(d => d.GalonesServidos).HasPrecision(18, 4);
        modelBuilder.Entity<Despacho>().Property(d => d.InventarioRestante).HasPrecision(18, 4);
        modelBuilder.Entity<Despacho>().Property(d => d.DisponibilidadRestante).HasPrecision(18, 4);
        modelBuilder.Entity<Despacho>().HasOne(d => d.Tanque).WithMany()
            .HasForeignKey(d => d.TanqueId).OnDelete(DeleteBehavior.Restrict);
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

        // CierreDiario — nuevas propiedades
        modelBuilder.Entity<CierreDiario>()
            .HasOne(c => c.CreadoPor)
            .WithMany()
            .HasForeignKey(c => c.CreadoPorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CierreDiario>().Property(c => c.TotalDespachos).HasDefaultValue(0);

        // CierreDiarioDetalle
        modelBuilder.Entity<CierreDiarioDetalle>()
            .HasOne(d => d.CierreDiario)
            .WithMany(c => c.Detalles)
            .HasForeignKey(d => d.CierreDiarioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CierreDiarioDetalle>()
            .HasOne(d => d.Tanque)
            .WithMany()
            .HasForeignKey(d => d.TanqueId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CierreDiarioDetalle>()
            .HasIndex(d => new { d.CierreDiarioId, d.TanqueId }).IsUnique();
        modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.VolumenDespachado).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.VolumenRecibido).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.InventarioInicial).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.InventarioFinal).HasPrecision(18, 4);
        modelBuilder.Entity<CierreDiarioDetalle>().Property(d => d.Diferencias).HasPrecision(18, 4);

        base.OnModelCreating(modelBuilder);
    }
}
