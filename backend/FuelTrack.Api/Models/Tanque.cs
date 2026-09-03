namespace FuelTrack.Api.Models;

public class Tanque
{
    public int Id { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public decimal Capacidad { get; set; }
    public decimal NivelActual { get; set; }
    public decimal NivelCritico { get; set; }
    public bool Activo { get; set; } = true;

    public int TipoCombustibleId { get; set; }
    public TipoCombustible TipoCombustible { get; set; } = null!;

    public Inventario? Inventario { get; set; }
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    public ICollection<RecepcionCombustible> Recepciones { get; set; } = new List<RecepcionCombustible>();
}
