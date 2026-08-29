using FuelTrack.Api.Models.Enums;

namespace FuelTrack.Api.Models;

public class MovimientoInventario
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Volumen { get; set; }
    public DateTime FechaHora { get; set; }
    public string? ReferenciaOperacion { get; set; }
    public string? Observaciones { get; set; }

    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
