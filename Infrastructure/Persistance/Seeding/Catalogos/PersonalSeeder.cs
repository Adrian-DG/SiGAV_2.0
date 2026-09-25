using Domain.Entities.Misc;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Catalogos;

/// <summary>Rangos, departamentos y nacionalidades. Cada tabla se revisa por separado.</summary>
internal sealed class PersonalSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Catalogo;
    public int Orden => 20;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (!await context.Rangos.AnyAsync(cancellationToken))
            context.Rangos.AddRange(PersonalData.Rangos.Select(r => new Rango
            {
                Nombre = r.Nombre,
                NombreArmada = r.NombreArmada,
                IsActive = true
            }));

        if (!await context.Departamentos.AnyAsync(cancellationToken))
            context.Departamentos.AddRange(PersonalData.Departamentos.Select(nombre => new Departamento { Nombre = nombre, IsActive = true }));

        if (!await context.Nacionalidades.AnyAsync(cancellationToken))
            context.Nacionalidades.AddRange(PersonalData.Nacionalidades.Select(nombre => new Nacionalidad { Nombre = nombre, IsActive = true }));

        return await context.SaveChangesAsync(cancellationToken);
    }
}
