using Application.Contracts.Notificaciones;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

/// <summary>Envía las notificaciones por <see cref="NotificacionesHub"/> desde fuera del hub (handlers).</summary>
public sealed class NotificadorUnidades(IHubContext<NotificacionesHub> hub) : INotificadorUnidades
{
    public Task NotificarTodasAsync(NotificacionUnidad notificacion, CancellationToken cancellationToken = default)
        => hub.Clients.Group(NotificacionesHub.GrupoUnidades)
            .SendAsync(NotificacionesHub.MetodoNotificacion, notificacion, cancellationToken);

    public Task NotificarUnidadesAsync(IReadOnlyCollection<int> unidadIds, NotificacionUnidad notificacion, CancellationToken cancellationToken = default)
    {
        if (unidadIds.Count == 0) return Task.CompletedTask;

        var grupos = unidadIds.Distinct().Select(NotificacionesHub.GrupoUnidad).ToList();
        return hub.Clients.Groups(grupos).SendAsync(NotificacionesHub.MetodoNotificacion, notificacion, cancellationToken);
    }
}
