using Application.Contracts.Authentication;
using Application.Features.Usuarios;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

namespace Presentation.Controllers.Authentication;

/// <summary>Usuarios del front desk y los permisos (roles de Identity) que tiene cada uno.</summary>
[RequierePermiso(Permisos.UsuariosGestionar)]
[Route("api/usuarios")]
public class UsuariosController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>Listado paginado; busca por usuario, cédula, nombre o apellido.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetUsuariosQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Alta de un usuario del front desk, con sus permisos iniciales.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>Listas del formulario de alta: rangos y departamentos activos.</summary>
    [HttpGet("catalogos")]
    public async Task<IActionResult> GetCatalogos(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetCatalogosUsuarioQuery(), cancellationToken));

    /// <summary>Catálogo de permisos que se pueden asignar.</summary>
    [HttpGet("permisos")]
    public async Task<IActionResult> GetPermisos(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetPermisosQuery(), cancellationToken));

    /// <summary>
    /// Reemplaza los permisos del usuario. Llegan a su sesión cuando vuelve a iniciar sesión (viajan en el token).
    /// </summary>
    [HttpPut("{id:int}/permisos")]
    public async Task<IActionResult> AsignarPermisos([FromRoute] int id, [FromBody] AsignarPermisosRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AsignarPermisosUsuarioCommand(id, request.Permisos), cancellationToken);
        return NoContent();
    }

    public record AsignarPermisosRequest(IReadOnlyList<string> Permisos);
}
