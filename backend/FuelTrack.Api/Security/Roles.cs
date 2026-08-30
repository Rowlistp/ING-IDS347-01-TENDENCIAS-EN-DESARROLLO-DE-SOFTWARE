namespace FuelTrack.Api.Security;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Supervisor = "Supervisor";
    public const string Despachador = "Despachador";
    public const string Auditor = "Auditor";
    public const string Consulta = "Consulta";
    public const string Solicitante = "Solicitante";

    public static readonly string[] Todos =
    [
        Administrador,
        Supervisor,
        Despachador,
        Auditor,
        Consulta,
        Solicitante
    ];
}
