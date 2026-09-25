using Application.Contracts.Authentication;
using Application.Features.Catalogos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Catalogos;

/// <summary>Catálogos activos para los formularios (ordenados por nombre).</summary>
[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api/catalogos")]
public class CatalogosController(IMediator mediator) : GenericController(mediator)
{
    [HttpGet("tipos-evento")]
    public async Task<IActionResult> TiposEvento(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetTiposEventoQuery(), cancellationToken));

    [HttpGet("provincias")]
    public Task<IActionResult> Provincias(CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.Provincias), cancellationToken);

    [HttpGet("provincias/{provinciaId:int}/municipios")]
    public Task<IActionResult> Municipios([FromRoute] int provinciaId, CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.Municipios, ProvinciaId: provinciaId), cancellationToken);

    [HttpGet("tipos-vehiculo")]
    public Task<IActionResult> TiposVehiculo(CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.TiposVehiculo), cancellationToken);

    [HttpGet("marcas")]
    public Task<IActionResult> Marcas(CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.Marcas), cancellationToken);

    [HttpGet("marcas/{marcaId:int}/modelos")]
    public Task<IActionResult> Modelos([FromRoute] int marcaId, [FromQuery] int? tipoVehiculoId, CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.Modelos, MarcaId: marcaId, TipoVehiculoId: tipoVehiculoId), cancellationToken);

    [HttpGet("colores")]
    public Task<IActionResult> Colores(CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.Colores), cancellationToken);

    [HttpGet("nacionalidades")]
    public Task<IActionResult> Nacionalidades(CancellationToken cancellationToken)
        => Listar(new GetCatalogoQuery(CatalogoEnum.Nacionalidades), cancellationToken);

    private async Task<IActionResult> Listar(GetCatalogoQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));
}
