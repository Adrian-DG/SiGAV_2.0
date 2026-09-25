using Application.Common.Models;
using Application.Features.Operaciones.Eventos;
using Domain.Enums;

namespace Application.Contracts.Operaciones;

/// <param name="UnidadId">Si tiene valor, solo eventos en los que participa esa unidad (sesión móvil).</param>
/// <param name="DesdeUtc">Inclusivo.</param>
/// <param name="HastaExclusivoUtc">Exclusivo.</param>
public record FiltroEventos(
    EstadoEventoEnum? Estado,
    DateTime? DesdeUtc,
    DateTime? HastaExclusivoUtc,
    int? UnidadId,
    bool IncluirAnulados);

public interface IEventoQueries
{
    Task<PagedResult<EventoListItemViewModel>> GetPagedAsync(FiltroEventos filtro, int page, int size, CancellationToken cancellationToken = default);

    /// <summary>null si no existe o si <paramref name="unidadId"/> no participa en el evento.</summary>
    Task<EventoDetalleViewModel?> GetDetalleAsync(int eventoId, int? unidadId, CancellationToken cancellationToken = default);
}
