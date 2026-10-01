using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

/// <summary>
/// Denominación con la que opera la unidad. <see cref="EsEncargado"/>: el nivel es de supervisión
/// (Supervisor Regional o Encargado de Tramo, NivelDenominacion.EsEncargado), que ve estadísticas
/// más allá de su propia unidad.
/// </summary>
public record DenominacionActualViewModel(
    int Id,
    string Nombre,
    int TramoId,
    string Tramo,
    string Nivel,
    JerarquiaEnum Jerarquia,
    bool EsEncargado);

// Query
public record GetDenominacionActualQuery(int UnidadId) : IRequest<DenominacionActualViewModel>;

// Handler
public class GetDenominacionActualQueryHandler(IReadDbContext db) : IRequestHandler<GetDenominacionActualQuery, DenominacionActualViewModel>
{
    public async Task<DenominacionActualViewModel> Handle(GetDenominacionActualQuery request, CancellationToken cancellationToken)
    {
        // En SiGAV 1.0 esto lanzaba NullReferenceException si la unidad no existía o no tenía denominación
        return await db.Unidades
            .Where(u => u.Id == request.UnidadId && u.Denominacion != null)
            .Select(u => new DenominacionActualViewModel(
                u.Denominacion!.Id,
                u.Denominacion.Nombre,
                u.Denominacion.TramoId,
                u.Denominacion.Tramo!.Nombre,
                u.Denominacion.Nivel!.Nombre,
                u.Denominacion.Nivel.Jerarquia,
                // Igual que NivelDenominacion.EsEncargado (propiedad calculada, EF no la traduce)
                u.Denominacion.Nivel.Jerarquia != JerarquiaEnum.Unidad))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"La unidad '{request.UnidadId}' no existe o no tiene denominación asignada.");
    }
}
