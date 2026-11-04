using Application.Contracts.Authentication;
using Application.Features.Calidad;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Calidad;

/// <summary>
/// Versiones de la encuesta de calidad: nueva versión (borrador copiado de la vigente), edición del
/// borrador, publicación y descarte. Una versión publicada no se modifica.
/// </summary>
[Authorize(Policy = SesionPolicies.Web)]
[RequierePermiso(Permisos.CalidadEncuestas)]
[Route("api/calidad/encuestas")]
public class EncuestasController(IMediator mediator) : GenericController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEncuestasQuery(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEncuestaQuery(id), cancellationToken));

    [HttpPost("nueva-version")]
    public async Task<IActionResult> CrearVersion(CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(new CrearVersionEncuestaCommand(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarBorrador([FromRoute] int id, [FromBody] ActualizarBorradorEncuestaCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { EncuestaId = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/publicar")]
    public async Task<IActionResult> Publicar([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new PublicarEncuestaCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Descartar([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DescartarBorradorEncuestaCommand(id), cancellationToken);
        return NoContent();
    }
}
