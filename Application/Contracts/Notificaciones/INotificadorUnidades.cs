namespace Application.Contracts.Notificaciones;

/// <summary>
/// Envía notificaciones en tiempo real (SignalR) a la app móvil de las unidades en campo. Solo las
/// reciben las unidades con la app abierta y conectada al hub en ese momento: no hay cola ni
/// reintento, así que lo que la unidad deba ver igual (p. ej. un evento asignado) se consulta
/// además desde la API.
/// </summary>
public interface INotificadorUnidades
{
    /// <summary>A todas las unidades conectadas.</summary>
    Task NotificarTodasAsync(NotificacionUnidad notificacion, CancellationToken cancellationToken = default);

    /// <summary>Solo a las unidades indicadas (las que no estén conectadas no la reciben).</summary>
    Task NotificarUnidadesAsync(IReadOnlyCollection<int> unidadIds, NotificacionUnidad notificacion, CancellationToken cancellationToken = default);
}

/// <summary>Unidades con la app conectada al hub de notificaciones en este momento.</summary>
public interface IConexionesUnidades
{
    bool EstaConectada(int unidadId);

    IReadOnlyCollection<int> Conectadas();
}

/// <summary>
/// Lo que recibe la app (método "notificacion" del hub). <see cref="EventoId"/> y
/// <see cref="AgenteId"/> solo en las notificaciones de un evento: el agente que debe atenderlo.
/// </summary>
public sealed record NotificacionUnidad(
    string Tipo,
    string Titulo,
    string Mensaje,
    DateTime FechaUtc,
    int? EventoId = null,
    int? AgenteId = null);

public static class TiposNotificacion
{
    /// <summary>Mensaje del centro de operaciones (call center).</summary>
    public const string General = "general";

    /// <summary>El call center asignó un evento a la unidad: queda pendiente en la app.</summary>
    public const string EventoAsignado = "evento-asignado";

    /// <summary>Un evento asignado a la unidad se anuló antes de completarse.</summary>
    public const string EventoAnulado = "evento-anulado";
}
