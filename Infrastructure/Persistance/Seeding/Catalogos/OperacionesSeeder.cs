using Domain.Entities.Operaciones;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Catalogos;

/// <summary>
/// Zonas, tramos, niveles de denominación, tipos de evento y tipos de cierre. Las denominaciones
/// y unidades no son catálogo: las reales se migran del legacy (ver <c>Demo</c> para las de prueba).
/// </summary>
internal sealed class OperacionesSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Catalogo;
    public int Orden => 40;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var insertados = 0;

        if (!await context.Zonas.AnyAsync(cancellationToken))
        {
            var zonas = OperacionesData.Zonas
                .Select(r => new Zona { Nombre = r.Nombre, Region = r.Macro, IsActive = true })
                .ToList();
            context.Zonas.AddRange(zonas);
            insertados += await context.SaveChangesAsync(cancellationToken);

            var zonaPorNombre = zonas.ToDictionary(z => z.Nombre);
            context.Tramos.AddRange(OperacionesData.Tramos.Select(t => new Tramo
            {
                Nombre = t.Nombre,
                ZonaId = zonaPorNombre[t.Zona].Id,
                IsActive = true
            }));
        }

        if (!await context.NivelesDenominacion.AnyAsync(cancellationToken))
            context.NivelesDenominacion.AddRange(OperacionesData.NivelesDenominacion.Select(n => new NivelDenominacion
            {
                Nombre = n.Nombre,
                Jerarquia = n.Jerarquia,
                EsAmbulancia = n.EsAmbulancia,
                IsActive = true
            }));

        if (!await context.TipoEventos.AnyAsync(cancellationToken))
            context.TipoEventos.AddRange(OperacionesData.TiposEvento.Select(t => new TipoEvento
            {
                Nombre = t.Nombre,
                Categoria = t.Categoria,
                IsActive = true
            }));

        if (!await context.TiposCierre.AnyAsync(cancellationToken))
            context.TiposCierre.AddRange(OperacionesData.TiposCierre.Select(nombre => new TipoCierre
            {
                Nombre = nombre,
                IsActive = true
            }));

        return insertados + await context.SaveChangesAsync(cancellationToken);
    }
}
