using Application.Contracts.Authentication;
using Application.Features.Operaciones.Eventos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Operaciones;

/// <summary>
/// Eventos (Asistencias en SiGAV 1.0). Front desk opera sobre cualquier evento; la app móvil solo
/// sobre los de su unidad, y para cambiar el estado su unidad debe ser la principal.
/// </summary>
[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api/eventos")]
public class EventosController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Listado paginado (más recientes primero). desde/hasta: días operativos de RD (yyyy-MM-dd).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetEventosQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEventoQuery(id), cancellationToken));

    /// <summary>
    /// Registra un evento. Idempotente por requestId: un reenvío devuelve 200 con el evento existente
    /// y esDuplicado = true; un registro nuevo devuelve 201.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEventoCommand command, CancellationToken cancellationToken)
    {
        var resultado = await Mediator.Send(command, cancellationToken);
        return resultado.EsDuplicado
            ? Ok(resultado)
            : CreatedAtAction(nameof(Get), new { id = resultado.Id }, resultado);
    }

    /// <summary>La unidad llegó al lugar (Pendiente → En curso). Sin fecha se usa la hora actual.</summary>
    [HttpPatch("{id:int}/iniciar")]
    public async Task<IActionResult> Iniciar([FromRoute] int id, [FromBody] IniciarRequest? request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new IniciarAtencionEventoCommand(id, request?.FechaHoraLlegadaUtc), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/completar")]
    public async Task<IActionResult> Completar([FromRoute] int id, [FromBody] CompletarRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new CompletarEventoCommand(id, request.TipoCierre, request.FechaHoraCompletadoUtc), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPatch("{id:int}/anular")]
    public async Task<IActionResult> Anular([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AnularEventoCommand(id), cancellationToken);
        return NoContent();
    }

    public record IniciarRequest(DateTime? FechaHoraLlegadaUtc);

    public record CompletarRequest(Domain.Enums.TipoCierreEventoEnum TipoCierre, DateTime? FechaHoraCompletadoUtc);
}
