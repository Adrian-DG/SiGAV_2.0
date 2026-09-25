using Application.Contracts;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Denominaciones;

// Query: denominación con su nivel, jerarquía y asignaciones de supervisión
public record GetDenominacionDetalleQuery(int DenominacionId) : IRequest<DenominacionDetalleViewModel>;

// Handler
public class GetDenominacionDetalleQueryHandler(IReadDbContext db)
    : IRequestHandler<GetDenominacionDetalleQuery, DenominacionDetalleViewModel>
{
    public async Task<DenominacionDetalleViewModel> Handle(GetDenominacionDetalleQuery request, CancellationToken cancellationToken)
    {
        var detalle = await db.Denominaciones
            .Where(d => d.Id == request.DenominacionId && d.IsActive)
            .Select(d => new
            {
                d.Id,
                d.Nombre,
                d.NivelDenominacionId,
                Nivel = d.Nivel!.Nombre,
                d.Nivel.Jerarquia,
                d.TramoId,
                Tramo = d.Tramo!.Nombre,
                Macros = d.Regiones.Where(r => r.RegionMacro != null).Select(r => r.RegionMacro!.Value).ToList(),
                Regiones = d.Regiones
                    .Where(r => r.RegionAsistenciaId != null)
                    .Select(r => new AsignacionViewModel(r.RegionAsistencia!.Id, r.RegionAsistencia.Nombre))
                    .ToList(),
                Tramos = d.Tramos.Select(t => new AsignacionViewModel(t.Tramo!.Id, t.Tramo.Nombre)).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("La denominación", request.DenominacionId);

        return new DenominacionDetalleViewModel(
            detalle.Id,
            detalle.Nombre,
            detalle.NivelDenominacionId,
            detalle.Nivel,
            detalle.Jerarquia,
            detalle.TramoId,
            detalle.Tramo,
            detalle.Macros.Order().ToList(),
            detalle.Regiones.OrderBy(r => r.Nombre).ToList(),
            detalle.Tramos.OrderBy(t => t.Nombre).ToList());
    }
}
