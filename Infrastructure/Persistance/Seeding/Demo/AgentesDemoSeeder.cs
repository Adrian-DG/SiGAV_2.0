using Domain.Entities.Operaciones;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Demo;

/// <summary>Agentes de prueba para iniciar sesión en la app móvil.</summary>
internal sealed class AgentesDemoSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Demo;
    public int Orden => 210;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Agentes.AnyAsync(cancellationToken)) return 0;

        var rangos = await context.Rangos.ToDictionaryAsync(r => r.Nombre, r => r.Id, cancellationToken);

        context.Agentes.AddRange(DemoData.Agentes.Select(a => Agente.Crear(
            a.Identificacion,
            a.Nombre,
            a.Apellido,
            a.Sexo,
            a.Institucion,
            rangos[a.Rango],
            a.Area,
            a.AccesoTotal,
            a.Especialidad,
            a.Autorizado)));

        return await context.SaveChangesAsync(cancellationToken);
    }
}
