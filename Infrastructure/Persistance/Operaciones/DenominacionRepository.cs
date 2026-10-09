using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class DenominacionRepository(SiGAVContext context) : IDenominacionRepository
{
    public Task<Denominacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.Denominaciones
            .Include(d => d.Nivel)
            .Include(d => d.Regiones)
            .Include(d => d.Tramos)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive, cancellationToken);

    public Task<NivelDenominacion?> GetNivelAsync(int nivelDenominacionId, CancellationToken cancellationToken = default)
        => context.NivelesDenominacion.FirstOrDefaultAsync(n => n.Id == nivelDenominacionId && n.IsActive, cancellationToken);

    /// <summary>
    /// Sin distinguir mayúsculas ("samaná móvil i" = "Samaná Móvil I"). Se compara en .NET: upper()
    /// de SQLite solo convierte ASCII y no igualaría "á" con "Á". El largo acota las candidatas.
    /// </summary>
    public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var candidatas = await context.Denominaciones
            .Where(d => d.Nombre.Length == nombre.Length)
            .Select(d => d.Nombre)
            .ToListAsync(cancellationToken);
        return candidatas.Any(n => string.Equals(n, nombre, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>Misma comparación que <see cref="ExisteNombreAsync"/> (en .NET, acotada por largo).</summary>
    public async Task<List<Denominacion>> GetByNombresAsync(IReadOnlyCollection<string> nombres, CancellationToken cancellationToken = default)
    {
        var largos = nombres.Select(n => n.Length).Distinct().ToList();
        var candidatas = await context.Denominaciones
            .Where(d => largos.Contains(d.Nombre.Length))
            .ToListAsync(cancellationToken);
        return candidatas
            .Where(d => nombres.Any(n => string.Equals(n, d.Nombre, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
    }

    public void Add(Denominacion denominacion) => context.Denominaciones.Add(denominacion);
}
