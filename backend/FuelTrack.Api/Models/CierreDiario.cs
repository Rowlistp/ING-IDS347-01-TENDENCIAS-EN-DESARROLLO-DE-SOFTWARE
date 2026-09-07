namespace FuelTrack.Api.Models;

public class CierreDiario
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal VolumenDespachado { get; set; }
    public decimal InventarioFinal { get; set; }
    public decimal Diferencias { get; set; }
    public int TotalDespachos { get; set; }
    public byte[]? PdfActa { get; set; }
    public int CreadoPorId { get; set; }
    public Usuario CreadoPor { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
    public ICollection<CierreDiarioDetalle> Detalles { get; set; } = [];
}
