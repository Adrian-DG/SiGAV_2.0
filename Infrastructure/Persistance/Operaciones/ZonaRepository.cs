using Domain.Entities.Operaciones;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class ZonaRepository(SiGAVContext context) : IZonaRepository
{
    public Task<Zona?> GetZonaAsync(int id, CancellationToken cancellationToken = default)
        => context.Zonas.FirstOrDefaultAsync(z => z.Id == id && z.IsActive, cancellationToken);

    public Task<bool> ExisteNombreZonaAsync(string nombre, CancellationToken cancellationToken = default)
        => ExisteNombreAsync(context.Zonas.Select(z => z.Nombre), nombre, cancellationToken);

    public Task<bool> ExisteNombreTramoAsync(string nombre, CancellationToken cancellationToken = default)
        => ExisteNombreAsync(context.Tramos.Select(t => t.Nombre), nombre, cancellationToken);

    public void Add(Zona zona) => context.Zonas.Add(zona);

    public void Add(Tramo tramo) => context.Tramos.Add(tramo);

    /// <summary>
    /// Como en DenominacionRepository: se compara en .NET para no depender de la intercalación
    /// (collation) de la base; el largo acota las candidatas.
    /// </summary>
    private static async Task<bool> ExisteNombreAsync(IQueryable<string> nombres, string nombre, CancellationToken cancellationToken)
    {
        var candidatos = await nombres.Where(n => n.Length == nombre.Length).ToListAsync(cancellationToken);
        return candidatos.Any(n => string.Equals(n, nombre, StringComparison.CurrentCultureIgnoreCase));
    }
}
