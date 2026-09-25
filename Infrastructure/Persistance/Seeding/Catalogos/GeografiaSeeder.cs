using Domain.Entities.Misc;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Catalogos;

/// <summary>Provincias y municipios.</summary>
internal sealed class GeografiaSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Catalogo;
    public int Orden => 10;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Provincias.AnyAsync(cancellationToken)) return 0;

        var provincias = GeografiaData.Provincias
            .Select(nombre => new Provincia { Nombre = nombre, IsActive = true })
            .ToList();
        context.Provincias.AddRange(provincias);
        // Se guardan antes que los municipios para que los Ids sigan el orden de la lista
        await context.SaveChangesAsync(cancellationToken);

        var porNombre = provincias.ToDictionary(p => p.Nombre);
        context.Municipios.AddRange(GeografiaData.Municipios.Select(m => new Municipio
        {
            Nombre = m.Nombre,
            ProvinciaId = porNombre[m.Provincia].Id,
            IsActive = true
        }));

        return provincias.Count + await context.SaveChangesAsync(cancellationToken);
    }
}
