using Domain.Entities.Misc;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Demo;

/// <summary>
/// Centros de salud de ejemplo para probar el traslado de la atención prehospitalaria. En producción
/// el catálogo real se importa de la tabla Hospitales de SiGAV 1.0.
/// </summary>
internal sealed class HospitalesDemoSeeder(SiGAVContext context) : ISeeder
{
    private static readonly (string Nombre, string Provincia)[] Hospitales =
    [
        ("Hospital Demo Distrito Nacional", "DISTRITO NACIONAL"),
        ("Hospital Demo Santo Domingo Este", "SANTO DOMINGO"),
        ("Hospital Demo Santiago", "SANTIAGO"),
        ("Hospital Demo La Romana", "LA ROMANA"),
    ];

    public SeedCategoria Categoria => SeedCategoria.Demo;
    public int Orden => 230;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Hospitales.AnyAsync(cancellationToken)) return 0;

        var provincias = await context.Provincias.ToDictionaryAsync(p => p.Nombre.ToUpper(), p => p.Id, cancellationToken);
        context.Hospitales.AddRange(Hospitales.Select(h => new Hospital
        {
            Nombre = h.Nombre,
            ProvinciaId = provincias.TryGetValue(h.Provincia, out var id) ? id : null,
            IsActive = true
        }));

        return await context.SaveChangesAsync(cancellationToken);
    }
}
