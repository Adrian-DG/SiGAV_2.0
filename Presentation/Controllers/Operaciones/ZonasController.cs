using Application.Contracts.Authentication;
using Application.Features.Operaciones.Zonas;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Operaciones;

/// <summary>
/// Zonas (antes regiones de asistencia): pertenecen a una región (1 Este, 2 Norte, 3 Sur) y agrupan
/// tramos. Consultar: unidades.ver; crear: zonas.gestionar.
/// </summary>
[Authorize(Policy = SesionPolicies.Web)]
[Route("api/zonas")]
public class ZonasController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Zonas con sus tramos, por región y nombre. Sin incluirInactivos, solo las activas y sus tramos activos.</summary>
    [RequierePermiso(Permisos.UnidadesVer)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RegionMacroEnum? region, [FromQuery] bool incluirInactivos, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetZonasQuery(region, incluirInactivos), cancellationToken));

    /// <summary>Una zona con todos sus tramos.</summary>
    [RequierePermiso(Permisos.UnidadesVer)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetZonaQuery(id), cancellationToken));

    /// <summary>
    /// Crea una zona: nombre (único), región y, opcionalmente, los nombres de sus tramos (únicos entre
    /// todos los tramos). Se guarda todo junto. 409 si el nombre de la zona o de un tramo ya existe.
    /// </summary>
    [RequierePermiso(Permisos.ZonasGestionar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateZonaCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }
}

/// <summary>
/// Tramos de las zonas. Son los que se eligen al crear una denominación (y los que supervisa un
/// encargado de tramo). Consultar: unidades.ver; crear: zonas.gestionar.
/// </summary>
[Authorize(Policy = SesionPolicies.Web)]
[Route("api/tramos")]
public class TramosController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Tramos por zona, región o nombre, con su zona y cuántas denominaciones los usan.</summary>
    [RequierePermiso(Permisos.UnidadesVer)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetTramosQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Crea un tramo en una zona activa: nombre (único entre todos los tramos) y zonaId. 409 si el nombre ya existe.</summary>
    [RequierePermiso(Permisos.ZonasGestionar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTramoCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }
}
