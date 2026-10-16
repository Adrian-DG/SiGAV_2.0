using System.Globalization;
using Application.Contracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

/// <summary>
/// Hub de notificaciones en tiempo real (/hubs/notificaciones). La app móvil se conecta con su token
/// (solo sesiones móviles) y queda en el grupo de su unidad y en el de todas las unidades. Los
/// clientes no envían nada: solo escuchan el método <see cref="MetodoNotificacion"/>.
/// El token va en el query string (access_token): los WebSockets del navegador no envían headers.
/// </summary>
[Authorize(Policy = SesionPolicies.Movil)]
public class NotificacionesHub(ConexionesUnidades conexiones) : Hub
{
    public const string Ruta = "/hubs/notificaciones";
    public const string MetodoNotificacion = "notificacion";

    public const string GrupoUnidades = "unidades";

    public static string GrupoUnidad(int unidadId) => $"unidad-{unidadId}";

    public override async Task OnConnectedAsync()
    {
        if (UnidadDeLaSesion() is { } unidadId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoUnidad(unidadId));
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoUnidades);
            conexiones.Agregar(unidadId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Los grupos de SignalR se limpian solos al desconectarse; el registro de conexiones no
        if (UnidadDeLaSesion() is { } unidadId)
            conexiones.Quitar(unidadId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    private int? UnidadDeLaSesion()
    {
        var user = Context.User;
        if (user?.FindFirst(SesionClaims.TipoSesion)?.Value != TiposSesion.Movil) return null;

        return int.TryParse(user.FindFirst(SesionClaims.UnidadId)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unidadId)
            ? unidadId
            : null;
    }
}
