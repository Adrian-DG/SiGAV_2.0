using Application.Features.Operaciones.Historial;
using Application.Features.Operaciones.Posiciones;
using Application.Features.Operaciones.Unidades;
using Application.Contracts.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Operaciones;

[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api/unidades")]
public class UnidadesController(IMediator mediator) : GenericController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetUnidadesQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [HttpGet("autocomplete")]
    public async Task<IActionResult> AutoComplete([FromQuery] string? term, [FromQuery] bool esAmbulancia, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetUnidadesAutoCompleteQuery(term, esAmbulancia), cancellationToken));

    [HttpGet("tramo/{tramoId:int}")]
    public async Task<IActionResult> GetByTramo([FromRoute] int tramoId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetUnidadesPorTramoQuery(tramoId), cancellationToken));

    [HttpGet("{id:int}/denominacion-actual")]
    public async Task<IActionResult> GetDenominacionActual([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetDenominacionActualQuery(id), cancellationToken));

    /// <summary>Auditoría: cambios de denominación de la unidad (solo front desk).</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpGet("{id:int}/historial-denominaciones")]
    public async Task<IActionResult> GetHistorialDenominaciones([FromRoute] int id, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken cancellationToken = default)
        => Ok(await Mediator.Send(new GetHistorialDenominacionesUnidadQuery(id, page, size), cancellationToken));

    // Validación previa al login de la app: no hay token todavía.
    [AllowAnonymous]
    [HttpGet("confirm")]
    public async Task<IActionResult> ConfirmExiste([FromQuery] string? ficha, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ConfirmUnidadExisteQuery(ficha ?? string.Empty), cancellationToken));

    [HttpGet("confirm-disponibilidad")]
    public async Task<IActionResult> ConfirmDisponibilidad([FromQuery] string? ficha, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ConfirmUnidadDisponibleQuery(ficha ?? string.Empty), cancellationToken));

    /// <summary>Unidades, denominaciones (con la unidad que la ocupa), tramos y niveles activos para los formularios.</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpGet("opciones-asignacion")]
    public async Task<IActionResult> GetOpcionesAsignacion(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetOpcionesAsignacionQuery(), cancellationToken));

    /// <summary>
    /// Asigna una denominación a una unidad. Unidad: unidadId (existente) o nuevaUnidad {ficha, placa}.
    /// Denominación: denominacionId (existente) o nuevaDenominacion {nombre, tramoId, nivelDenominacionId}.
    /// Si otra unidad tenía la denominación, queda sin denominación y No disponible (unidadesLiberadas).
    /// </summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPost("asignar-denominacion")]
    public async Task<IActionResult> AsignarDenominacion([FromBody] AsignarDenominacionCommand command, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(command, cancellationToken));

    /// <summary>Crea una unidad; sin denominacionId queda sin denominación (No disponible).</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnidadCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPost("nueva-denominacion")]
    public async Task<IActionResult> CreateConNuevaDenominacion([FromBody] CreateUnidadConNuevaDenominacionCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateUnidadCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { UnidadId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>App móvil: posición actual de la unidad de la sesión (cada ~30 s). 403: la unidad ya no puede operar; 409: otro agente la opera.</summary>
    [Authorize(Policy = SesionPolicies.Movil)]
    [HttpPost("posicion")]
    public async Task<IActionResult> ReportarPosicion([FromBody] ReportarPosicionCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Mapa del front desk: última posición de cada unidad, con su agente, denominación y estado de conexión.</summary>
    [RequierePermiso(Permisos.UnidadesVer)]
    [HttpGet("posiciones")]
    public async Task<IActionResult> GetPosiciones(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetPosicionesUnidadesQuery(), cancellationToken));

    /// <summary>Cierra la sesión de la unidad para que otro agente pueda iniciarla sin esperar.</summary>
    [RequierePermiso(Permisos.UnidadesVer)]
    [HttpPost("{id:int}/liberar-sesion")]
    public async Task<IActionResult> LiberarSesion([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new LiberarSesionUnidadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{ficha}/disponibilidad")]
    public async Task<IActionResult> ToggleDisponibilidad([FromRoute] string ficha, CancellationToken cancellationToken)
        => Ok(new { estaDisponible = await Mediator.Send(new ToggleDisponibilidadUnidadCommand(ficha), cancellationToken) });

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DesactivarUnidadCommand(id), cancellationToken);
        return NoContent();
    }
}
