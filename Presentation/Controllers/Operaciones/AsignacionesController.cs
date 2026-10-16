using Application.Contracts.Authentication;
using Application.Features.Operaciones.Asignaciones;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Operaciones;

/// <summary>
/// Call center: crea eventos y los asigna a unidades disponibles, que reciben la notificación en
/// tiempo real por SignalR (/hubs/notificaciones). Los mensajes libres van por NotificacionesController.
/// </summary>
[RequierePermiso(Permisos.EventosAsignar)]
[Route("api/asignaciones")]
public class AsignacionesController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>
    /// Unidades con sesión abierta en la app, disponibles primero. Disponible: su agente, o su
    /// componente, no atiende ningún evento sin completar.
    /// </summary>
    [HttpGet("unidades")]
    public async Task<IActionResult> Unidades(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetUnidadesDisponiblesQuery(), cancellationToken));

    /// <summary>
    /// Crea el evento (pendiente) asignado a la unidad y le envía la notificación. 409 si la unidad
    /// ya no tiene sesión o el agente indicado ya atiende otro evento.
    /// </summary>
    [HttpPost("eventos")]
    public async Task<IActionResult> AsignarEvento([FromBody] AsignarEventoCommand command, CancellationToken cancellationToken)
    {
        var resultado = await Mediator.Send(command, cancellationToken);
        return Created($"/api/eventos/{resultado.Id}", resultado);
    }
}
