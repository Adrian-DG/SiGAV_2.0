using Application.Contracts.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class CatalogoQueries(SiGAVContext context) : ICatalogoQueries
{
    public Task<bool> ExisteTramoAsync(int tramoId, CancellationToken cancellationToken = default)
        => context.Tramos.AnyAsync(t => t.Id == tramoId && t.IsActive, cancellationToken);

    public Task<bool> ExisteNivelDenominacionAsync(int nivelDenominacionId, CancellationToken cancellationToken = default)
        => context.NivelesDenominacion.AnyAsync(n => n.Id == nivelDenominacionId && n.IsActive, cancellationToken);

    public Task<bool> ExisteRangoAsync(int rangoId, CancellationToken cancellationToken = default)
        => context.Rangos.AnyAsync(r => r.Id == rangoId && r.IsActive, cancellationToken);

    public Task<bool> ExisteMunicipioAsync(int municipioId, CancellationToken cancellationToken = default)
        => context.Municipios.AnyAsync(m => m.Id == municipioId && m.IsActive, cancellationToken);

    public async Task<IReadOnlyList<int>> TiposEventoInexistentesAsync(IReadOnlyCollection<int> tipoEventoIds, CancellationToken cancellationToken = default)
    {
        if (tipoEventoIds.Count == 0) return [];

        var existentes = await context.TipoEventos
            .Where(t => tipoEventoIds.Contains(t.Id) && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        return tipoEventoIds.Except(existentes).ToList();
    }

    public async Task<IReadOnlyList<int>> TramosInexistentesAsync(IReadOnlyCollection<int> tramoIds, CancellationToken cancellationToken = default)
    {
        if (tramoIds.Count == 0) return [];

        var existentes = await context.Tramos
            .Where(t => tramoIds.Contains(t.Id) && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        return tramoIds.Except(existentes).ToList();
    }

    public async Task<IReadOnlyList<int>> RegionesAsistenciaInexistentesAsync(IReadOnlyCollection<int> regionIds, CancellationToken cancellationToken = default)
    {
        if (regionIds.Count == 0) return [];

        var existentes = await context.Regiones
            .Where(r => regionIds.Contains(r.Id) && r.IsActive)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        return regionIds.Except(existentes).ToList();
    }
}
