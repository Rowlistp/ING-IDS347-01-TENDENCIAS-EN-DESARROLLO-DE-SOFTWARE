namespace FuelTrack.Api.Models;

public class CierreDiario
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal VolumenDespachado { get; set; }
    public decimal InventarioFinal { get; set; }
    public decimal Diferencias { get; set; }
    public string? ActaDigital { get; set; }
    public string? ReporteUrl { get; set; }
}
