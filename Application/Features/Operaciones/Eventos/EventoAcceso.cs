using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;

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

    /// <summary>Front desk, o una unidad que participa en el evento (con cualquier rol).</summary>
    public static void AsegurarParticipa(Evento evento, ICurrentUserService currentUser)
    {
        if (UnidadDeAlcance(currentUser) is { } unidadId && !evento.Unidades.Any(u => u.UnidadId == unidadId))
            throw new NotFoundException("El evento", evento.Id);
    }

    /// <summary>
    /// Agente de la sesión móvil que atiende el evento: el indicado, si es el agente de la sesión o
    /// su componente; sin indicar, el agente de la sesión. Que el componente siga activo y
    /// autorizado lo comprueba <see cref="AsegurarAgenteVigenteAsync"/>.
    /// </summary>
    public static int AgenteDeLaSesion(ICurrentUserService currentUser, int? solicitado)
    {
        var agenteId = currentUser.AgenteId ?? throw new ForbiddenException("La sesión no tiene un agente asociado.");
        if (solicitado is null || solicitado == agenteId) return agenteId;
        if (solicitado == currentUser.ComponenteId) return solicitado.Value;

        throw new ForbiddenException("El agente indicado no opera la unidad de la sesión.");
    }

    /// <summary>
    /// El token no se invalida si front desk desactiva o desautoriza al componente (solo se revisa
    /// al agente de la sesión en cada petición): se comprueba aquí antes de asignarle un evento.
    /// </summary>
    public static async Task AsegurarAgenteVigenteAsync(IAgenteRepository agentes, ICurrentUserService currentUser, int agenteId, CancellationToken cancellationToken)
    {
        if (agenteId == currentUser.AgenteId) return;

        if (await agentes.GetByIdAsync(agenteId, cancellationToken) is not { PuedeIniciarSesion: true })
            throw new ForbiddenException("El componente ya no está autorizado para operar. Comuníquese con front desk.");
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
