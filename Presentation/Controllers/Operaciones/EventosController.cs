using Application.Contracts.Authentication;
using Application.Features.Operaciones.Eventos;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Operaciones;

/// <summary>
/// Eventos (Asistencias en SiGAV 1.0). Front desk opera sobre cualquier evento; la app móvil solo
/// sobre los de su unidad, y para cambiar el estado su unidad debe ser la principal.
/// </summary>
[Authorize(Policy = SesionPolicies.Operativa)]
[Route("api/eventos")]
public class EventosController(IMediator mediator) : GenericController(mediator)
{
    /// <summary>
    /// Listado paginado (más recientes primero). desde/hasta: días operativos de RD (yyyy-MM-dd).
    /// agenteId/unidadId/denominacionId: eventos en que participan; ciudadano: cédula, nombre,
    /// apellido, teléfono o placa.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetEventosQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>
    /// Ubicación y categorías de los eventos del período, para el mapa de calor (solo front desk).
    /// Por defecto los últimos 30 días; como máximo 20 000 puntos (los más recientes).
    /// </summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpGet("mapa")]
    public async Task<IActionResult> Mapa([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetMapaEventosQuery(desde, hasta), cancellationToken));

    /// <summary>Opciones de los filtros del listado: agentes, unidades, denominaciones y tramos.</summary>
    [Authorize(Policy = SesionPolicies.Web)]
    [HttpGet("filtros")]
    public async Task<IActionResult> Filtros(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetFiltrosEventosQuery(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEventoQuery(id), cancellationToken));

    /// <summary>
    /// Registra un evento. Idempotente por requestId: un reenvío devuelve 200 con el evento existente
    /// y esDuplicado = true; un registro nuevo devuelve 201.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEventoCommand command, CancellationToken cancellationToken)
    {
        var resultado = await Mediator.Send(command, cancellationToken);
        return resultado.EsDuplicado
            ? Ok(resultado)
            : CreatedAtAction(nameof(Get), new { id = resultado.Id }, resultado);
    }

    /// <summary>La unidad llegó al lugar (Pendiente → En curso). Sin fecha se usa la hora actual.</summary>
    [HttpPatch("{id:int}/iniciar")]
    public async Task<IActionResult> Iniciar([FromRoute] int id, [FromBody] IniciarRequest? request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new IniciarAtencionEventoCommand(id, request?.FechaHoraLlegadaUtc), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/completar")]
    public async Task<IActionResult> Completar([FromRoute] int id, [FromBody] CompletarRequest request, CancellationToken cancellationToken)
    {
        await Mediator.Send(new CompletarEventoCommand(id, request.TipoCierreId, request.FechaHoraCompletadoUtc), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = SesionPolicies.Web)]
    [HttpPatch("{id:int}/anular")]
    public async Task<IActionResult> Anular([FromRoute] int id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new AnularEventoCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sube una evidencia (multipart/form-data): archivo (JPEG, PNG o WebP, máx. 10 MB), tipo
    /// (1 Foto, 2 Firma del ciudadano, 3 Firma del agente, 4 Foto de placa, 5 Foto de cédula) y,
    /// opcionales, requestId (idempotencia), ciudadanoId o vehiculoId (de este evento). La app solo
    /// puede subirla a eventos en que participa su unidad. Nueva: 201; reenvío: 200 con esDuplicado.
    /// </summary>
    [HttpPost("{id:int}/evidencias")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteSubida)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSubida)]
    public async Task<IActionResult> SubirEvidencia([FromRoute] int id, [FromForm] SubirEvidenciaRequest request, CancellationToken cancellationToken)
    {
        await using var contenido = request.Archivo?.OpenReadStream();
        var resultado = await Mediator.Send(
            new RegistrarEvidenciaCommand(id, request.Tipo, contenido!, request.RequestId, request.CiudadanoId, request.VehiculoId),
            cancellationToken);

        return resultado.EsDuplicado
            ? Ok(resultado)
            : CreatedAtAction(nameof(GetArchivoEvidencia), new { id, evidenciaId = resultado.Id }, resultado);
    }

    /// <summary>Archivo de la evidencia (la imagen), con su content type.</summary>
    [HttpGet("{id:int}/evidencias/{evidenciaId:int}/archivo")]
    public async Task<IActionResult> GetArchivoEvidencia([FromRoute] int id, [FromRoute] int evidenciaId, CancellationToken cancellationToken)
    {
        var archivo = await Mediator.Send(new GetArchivoEvidenciaQuery(id, evidenciaId), cancellationToken);

        // Una evidencia no cambia nunca: el navegador puede reutilizarla (solo él, no proxies)
        Response.Headers.CacheControl = "private, max-age=86400";
        Response.Headers.ContentDisposition = $"inline; filename=\"{archivo.NombreArchivo}\"";
        return File(archivo.Contenido, archivo.ContentType);
    }

    // Archivo más el resto del formulario
    private const long LimiteSubida = ArchivoEvidencia.MaxBytes + 1024 * 1024;

    public record SubirEvidenciaRequest(
        IFormFile? Archivo,
        TipoEvidenciaEnum Tipo,
        Guid? RequestId,
        int? CiudadanoId,
        int? VehiculoId);

    public record IniciarRequest(DateTime? FechaHoraLlegadaUtc);

    public record CompletarRequest(int TipoCierreId, DateTime? FechaHoraCompletadoUtc);
}
