namespace FuelTrack.Api.Models;

public class RecepcionCombustible
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public decimal VolumenRecibido { get; set; }
    public DateTime Fecha { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int TanqueId { get; set; }
    public Tanque Tanque { get; set; } = null!;

    // Cache del PDF generado (igual patrón que CierreDiario.PdfActa): se genera
    // una vez y se reutiliza en descargas posteriores en vez de recrearlo.
    public byte[]? PdfComprobante { get; set; }
}
