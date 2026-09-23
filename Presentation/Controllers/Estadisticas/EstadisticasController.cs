using Application.Contracts.Authentication;
using Application.Features.Estadisticas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Estadisticas;

[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api/estadisticas")]
public class EstadisticasController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>
    /// Eventos atendidos por tipo y categoría. El alcance lo decide la sesión: front desk ve todo;
    /// en la app, encargado regional → sus regiones, encargado de tramo → sus tramos,
    /// móvil/motorizada → solo su ficha. Fechas en formato yyyy-MM-dd.
    /// </summary>
    [HttpGet("eventos")]
    public async Task<IActionResult> GetEventos([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEstadisticasEventosQuery(desde, hasta), cancellationToken));
}
