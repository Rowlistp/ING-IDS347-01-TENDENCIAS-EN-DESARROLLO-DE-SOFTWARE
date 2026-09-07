namespace FuelTrack.Api.DTOs.Dashboard;

public sealed record DashboardResumenResponse(
    DashboardHoy Hoy,
    IReadOnlyList<DashboardDia> Ultimos7Dias,
    IReadOnlyList<DashboardTanque> Top3TanquesMasUsados,
    DashboardComparativaMes ComparativaMes,
    IReadOnlyList<DashboardDistribucion> DistribucionPorTipoCombustible,
    DashboardEficiencia EficienciaAprobacion);

public sealed record DashboardHoy(
    int TotalDespachos,
    decimal VolumenDespachado,
    int SolicitudesPendientes,
    int TanquesConInventarioBajo);

public sealed record DashboardDia(DateOnly Fecha, decimal VolumenDespachado, int TotalDespachos);

public sealed record DashboardTanque(int TanqueId, string Identificacion, decimal TotalGalones);

public sealed record DashboardComparativaMes(DashboardMes MesActual, DashboardMes MesAnterior);

public sealed record DashboardMes(decimal VolumenDespachado, int Solicitudes);

public sealed record DashboardDistribucion(string TipoCombustible, decimal Porcentaje);

public sealed record DashboardEficiencia(
    int Aprobadas,
    int Rechazadas,
    int Pendientes,
    decimal TasaAprobacion);
