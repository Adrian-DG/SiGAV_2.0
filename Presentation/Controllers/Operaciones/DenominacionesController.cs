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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDenominacionCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }
}
