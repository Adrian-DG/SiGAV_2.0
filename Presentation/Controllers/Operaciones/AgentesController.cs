using Application.Contracts.Authentication;
using Application.Features.Operaciones.Agentes;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Operaciones;

/// <summary>Agentes (Miembros en SiGAV 1.0): personal que opera las unidades desde la app.</summary>
[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api/agentes")]
public class AgentesController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Listado paginado. Use <c>autorizado=false</c> para ver los registros pendientes.</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAgentesQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetAgenteQuery(id), cancellationToken));

    /// <summary>Agentes activos y autorizados, opcionalmente de un área (p. ej. PreHospitalaria = 6).</summary>
    [HttpGet("autocomplete")]
    public async Task<IActionResult> AutoComplete([FromQuery] string? term, [FromQuery] AreaOperativaEnum? areaOperativa, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetAgentesAutoCompleteQuery(term, areaOperativa), cancellationToken));

    /// <summary>Validación previa al login de la app: indica si la cédula existe y está autorizada.</summary>
    [AllowAnonymous]
    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm([FromQuery] string? cedula, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ConfirmAgenteQuery(cedula ?? string.Empty), cancellationToken));

    /// <summary>Alta desde front desk: el agente queda autorizado.</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgenteCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>Auto-registro desde la app: el agente queda pendiente de autorización por front desk.</summary>
    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarAgenteCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return Accepted(new { message = "Registro recibido. Su acceso quedará habilitado cuando front desk lo autorice." });
    }

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAgenteCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command with { AgenteId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Autoriza (<c>true</c>) o revoca (<c>false</c>) el acceso del agente a la app.</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPatch("{id:int}/autorizacion")]
    public async Task<IActionResult> CambiarAutorizacion([FromRoute] int id, [FromBody] CambiarAutorizacionRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new CambiarAutorizacionAgenteCommand(id, request.Autorizado), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DesactivarAgenteCommand(id), cancellationToken);
        return NoContent();
    }

    public record CambiarAutorizacionRequest(bool Autorizado);
}
