using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Quién puede hacer qué con un evento:
/// - Front desk (web): cualquier evento.
/// - App móvil: solo eventos de su unidad; para cambiar el estado, su unidad debe ser la principal.
/// </summary>
internal static class EventoAcceso
{
    public static bool EsWeb(ICurrentUserService currentUser) => currentUser.TipoSesion == TiposSesion.Web;

    /// <summary>Unidad a la que se limita la consulta (sesión móvil), o null para front desk.</summary>
    public static int? UnidadDeAlcance(ICurrentUserService currentUser)
    {
        if (EsWeb(currentUser)) return null;

        return currentUser is { TipoSesion: TiposSesion.Movil, UnidadId: { } unidadId }
            ? unidadId
            : throw new ForbiddenException("La sesión actual no tiene acceso a eventos.");
    }

    public static void AsegurarPuedeOperar(Evento evento, ICurrentUserService currentUser)
    {
        if (EsWeb(currentUser)) return;

        var unidadId = UnidadDeAlcance(currentUser);
        var esPrincipal = evento.Unidades.Any(u => u.UnidadId == unidadId && u.Rol == RolUnidadEventoEnum.Principal);

        // Si la unidad ni siquiera participa se responde 404: no se revela que el evento existe
        if (!evento.Unidades.Any(u => u.UnidadId == unidadId))
            throw new NotFoundException("El evento", evento.Id);
        if (!esPrincipal)
            throw new ForbiddenException("Solo la unidad principal puede cambiar el estado del evento.");
    }
}
