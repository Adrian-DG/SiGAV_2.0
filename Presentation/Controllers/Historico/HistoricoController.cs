using Application.Contracts.Authentication;
using Application.Features.Historico;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Historico;

/// <summary>
/// Autocompletado del formulario de eventos: datos ya conocidos de una persona o un vehículo.
/// 404 = no se conoce (el agente llena los datos a mano). Solo sesiones autenticadas.
/// </summary>
[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api")]
public class HistoricoController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Por cédula o pasaporte (con o sin guiones).</summary>
    [HttpGet("ciudadanos/{identificacion}")]
    public async Task<IActionResult> Ciudadano([FromRoute] string identificacion, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetCiudadanoConocidoQuery(identificacion), cancellationToken));

    [HttpGet("vehiculos/{placa}")]
    public async Task<IActionResult> Vehiculo([FromRoute] string placa, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetVehiculoConocidoQuery(placa), cancellationToken));
}
