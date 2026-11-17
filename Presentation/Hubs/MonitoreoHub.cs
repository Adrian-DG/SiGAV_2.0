using Application.Contracts.Authentication;
using Application.Contracts.Notificaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

/// <summary>
/// Hub del front desk (/hubs/monitoreo): avisa en tiempo real qué eventos y unidades cambiaron
/// (<see cref="CambioTiempoReal"/>, método <see cref="MetodoCambio"/>) para que las pantallas
/// abiertas se recarguen. Solo sesiones web; cada conexión entra a los grupos de los tipos de
/// cambio que su usuario puede ver según sus permisos. Los clientes no envían nada: solo escuchan.
/// </summary>
[Authorize(Policy = SesionPolicies.Web)]
public class MonitoreoHub : Hub
{
    public const string Ruta = "/hubs/monitoreo";
    public const string MetodoCambio = "cambio";

    /// <summary>Permisos con los que se reciben los cambios de cada tipo (basta con uno).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> PermisosPorTipo = new Dictionary<string, string[]>
    {
        [TiposCambioTiempoReal.Eventos] = [Permisos.EventosVer, Permisos.EventosAsignar, Permisos.CalidadEvaluar],
        [TiposCambioTiempoReal.Unidades] = [Permisos.UnidadesVer, Permisos.EventosAsignar, Permisos.UnidadesNotificar],
    };

    public static string Grupo(string tipo) => $"monitoreo-{tipo}";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        foreach (var (tipo, permisos) in PermisosPorTipo)
        {
            if (user is not null && permisos.Any(p => user.HasClaim(SesionClaims.Permiso, p)))
                await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(tipo));
        }

        await base.OnConnectedAsync();
    }
}

/// <summary>Publica los cambios por <see cref="MonitoreoHub"/> desde fuera del hub.</summary>
public sealed class MonitorTiempoReal(IHubContext<MonitoreoHub> hub) : IMonitorTiempoReal
{
    public Task PublicarAsync(CambioTiempoReal cambio, CancellationToken cancellationToken = default)
        => hub.Clients.Group(MonitoreoHub.Grupo(cambio.Tipo))
            .SendAsync(MonitoreoHub.MetodoCambio, cambio, cancellationToken);
}
