namespace FuelTrack.Api.Models;

public class CierreDiarioDetalle
{
    public int Id { get; set; }
    public int CierreDiarioId { get; set; }
    public CierreDiario CierreDiario { get; set; } = null!;
    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;
    public int NumeroDespachos { get; set; }
    public decimal VolumenDespachado { get; set; }
    public decimal VolumenRecibido { get; set; }
    public decimal InventarioInicial { get; set; }
    public decimal InventarioFinal { get; set; }
    public decimal Diferencias { get; set; }
}
