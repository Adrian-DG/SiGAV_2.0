using Domain.Entities.Operaciones;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Catalogos;

/// <summary>
/// Regiones de asistencia, tramos, niveles de denominación y tipos de evento. Las denominaciones
/// y unidades no son catálogo: las reales se migran del legacy (ver <c>Demo</c> para las de prueba).
/// </summary>
internal sealed class OperacionesSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Catalogo;
    public int Orden => 40;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var insertados = 0;

        if (!await context.Regiones.AnyAsync(cancellationToken))
        {
            var regiones = OperacionesData.Regiones
                .Select(r => new RegionAsistencia { Nombre = r.Nombre, Region = r.Macro, IsActive = true })
                .ToList();
            context.Regiones.AddRange(regiones);
            insertados += await context.SaveChangesAsync(cancellationToken);

            var regionPorNombre = regiones.ToDictionary(r => r.Nombre);
            context.Tramos.AddRange(OperacionesData.Tramos.Select(t => new Tramo
            {
                Nombre = t.Nombre,
                RegionAsistenciaId = regionPorNombre[t.Region].Id,
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

        return insertados + await context.SaveChangesAsync(cancellationToken);
    }
}
