using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Features.Calidad;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Calidad;

/// <summary>
/// Evaluación de calidad de los eventos completados: el evaluador contacta al ciudadano y registra la
/// encuesta vigente. Las rutas usan el Id del evento (una evaluación por evento).
/// </summary>
[Authorize(Policy = SesionPolicies.Web)]
[RequierePermiso(Permisos.CalidadEvaluar)]
[Route("api/calidad")]
public class CalidadController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Bandeja. estado: 0 = pendientes, 1 = en gestión, 2 = completadas, 3 = no contactado, 4 = rechazadas.</summary>
    [HttpGet("eventos")]
    public async Task<IActionResult> GetBandeja([FromQuery] GetBandejaCalidadQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [HttpGet("eventos/{eventoId:int}")]
    public async Task<IActionResult> GetEvaluacion([FromRoute] int eventoId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEvaluacionEventoQuery(eventoId), cancellationToken));

    [HttpPost("eventos/{eventoId:int}/evaluacion")]
    public async Task<IActionResult> Iniciar([FromRoute] int eventoId, [FromBody] IniciarEvaluacionCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command with { EventoId = eventoId }, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPut("eventos/{eventoId:int}/evaluacion/contacto")]
    public async Task<IActionResult> CambiarContacto([FromRoute] int eventoId, [FromBody] CambiarContactoEvaluacionCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { EventoId = eventoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("eventos/{eventoId:int}/evaluacion/intentos")]
    public async Task<IActionResult> RegistrarIntento([FromRoute] int eventoId, [FromBody] RegistrarIntentoContactoCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { EventoId = eventoId }, cancellationToken);
        return NoContent();
    }

    /// <summary>Guarda respuestas (parciales o todas); con completar = true cierra la evaluación.</summary>
    [HttpPut("eventos/{eventoId:int}/evaluacion/respuestas")]
    public async Task<IActionResult> GuardarRespuestas([FromRoute] int eventoId, [FromBody] GuardarRespuestasEvaluacionCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { EventoId = eventoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("eventos/{eventoId:int}/evaluacion/no-contactado")]
    public async Task<IActionResult> MarcarNoContactado([FromRoute] int eventoId, [FromBody] MarcarNoContactadoCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { EventoId = eventoId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("eventos/{eventoId:int}/evaluacion/reabrir")]
    public async Task<IActionResult> Reabrir([FromRoute] int eventoId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ReabrirEvaluacionCommand(eventoId), cancellationToken);
        return NoContent();
    }

    /// <summary>Excel de las evaluaciones de una versión de la encuesta (por defecto la vigente).</summary>
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar([FromQuery] ExportarEvaluacionesQuery query, CancellationToken cancellationToken)
    {
        var archivo = await Mediator.Send(query, cancellationToken);
        return File(archivo.Contenido, IHojaCalculo.ContentType, archivo.Nombre);
    }

    /// <summary>Versiones de la encuesta (para elegir cuál exportar). La administración está en EncuestasController.</summary>
    [HttpGet("versiones")]
    public async Task<IActionResult> GetVersiones(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEncuestasQuery(), cancellationToken));
}
