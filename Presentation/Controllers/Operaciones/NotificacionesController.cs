using Application.Contracts.Authentication;
using Application.Features.Operaciones.Asignaciones;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Operaciones;

/// <summary>
/// Mensajes del centro de operaciones a las unidades en campo, en tiempo real por SignalR
/// (/hubs/notificaciones). Solo los recibe la unidad que tenga la app conectada.
/// </summary>
[RequierePermiso(Permisos.UnidadesNotificar)]
[Route("api/notificaciones")]
public class NotificacionesController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Unidades con sesión abierta en la app y si tienen las notificaciones conectadas.</summary>
    [HttpGet("unidades")]
    public async Task<IActionResult> Unidades(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetUnidadesDisponiblesQuery(), cancellationToken));

    /// <summary>Notificación a las unidades indicadas (unidadIds) o, sin indicar, a todas las conectadas.</summary>
    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] EnviarNotificacionCommand command, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(command, cancellationToken));
}
