using Application.Features.Operaciones.Denominaciones;
using Application.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Operaciones;

[Authorize(Policy = SesionPolicies.Web)]
[Route("api/denominaciones")]
public class DenominacionesController(IMediator mediator) : GenericController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetDenominacionesQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetDenominacionDetalleQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDenominacionCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>Reemplaza las regiones (encargado regional) o tramos (encargado de tramo) supervisados.</summary>
    [HttpPut("{id:int}/asignaciones")]
    public async Task<IActionResult> DefinirAsignaciones([FromRoute] int id, [FromBody] DefinirAsignacionesDenominacionCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { DenominacionId = id }, cancellationToken);
        return NoContent();
    }
}
