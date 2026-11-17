namespace Application.Contracts.Notificaciones;

/// <summary>
/// Avisa en tiempo real (SignalR) al front desk que cambiaron datos operativos, para que las
/// pantallas abiertas (eventos, mapa de unidades, dashboard...) se recarguen sin esperar al
/// refresco periódico. Solo lleva qué cambió (tipo + ids), no los datos: cada pantalla vuelve a
/// pedir lo suyo a la API con sus filtros y permisos. Es "best effort": si falla no afecta la operación.
/// </summary>
public interface IMonitorTiempoReal
{
    Task PublicarAsync(CambioTiempoReal cambio, CancellationToken cancellationToken = default);
}

/// <summary>Lo que recibe el front desk (método "cambio" del hub de monitoreo).</summary>
public sealed record CambioTiempoReal(string Tipo, IReadOnlyCollection<int> Ids, DateTime FechaUtc);

public static class TiposCambioTiempoReal
{
    /// <summary>Se registró, actualizó, asignó, inició, completó o anuló un evento (o sus involucrados/evidencias).</summary>
    public const string Eventos = "eventos";

    /// <summary>Cambió una unidad: posición GPS, sesión, disponibilidad o denominación.</summary>
    public const string Unidades = "unidades";
}
