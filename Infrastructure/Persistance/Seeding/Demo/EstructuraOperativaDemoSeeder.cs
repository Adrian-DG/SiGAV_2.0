using Domain.Entities.Operaciones;
using Domain.Services;
using Domain.ValueObjects;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistance.Seeding.Demo;

/// <summary>
/// Denominaciones y unidades de prueba. Pasa por el dominio (Crear, DefinirAsignaciones y
/// <see cref="AsignacionDenominacionService"/>), así cada asignación queda en el historial
/// como "Carga inicial" a nombre del administrador.
/// </summary>
internal sealed class EstructuraOperativaDemoSeeder(
    SiGAVContext context,
    IOptions<SeedingOptions> options,
    TimeProvider timeProvider,
    ILogger<EstructuraOperativaDemoSeeder> logger) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Demo;
    public int Orden => 200;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Denominaciones.AnyAsync(cancellationToken) || await context.Unidades.AnyAsync(cancellationToken))
            return 0;

        // Todo cambio de denominación exige un usuario responsable de la web
        var adminId = await context.Users
            .Where(u => u.UserName == options.Value.Admin.UserName)
            .Select(u => (int?)u.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (adminId is null)
        {
            logger.LogWarning("Sin el usuario administrador no se cargan denominaciones ni unidades de prueba.");
            return 0;
        }

        var autor = new AutorCambio(adminId.Value, timeProvider.GetUtcNow().UtcDateTime, "Carga inicial");

        // Quedan en seguimiento: al agregar una denominación EF enlaza su Nivel, que
        // DefinirAsignaciones necesita para validar la jerarquía.
        var niveles = await context.NivelesDenominacion.ToDictionaryAsync(n => n.Nombre, cancellationToken);
        var tramos = await context.Tramos.ToDictionaryAsync(t => t.Nombre, t => t.Id, cancellationToken);
        var regiones = await context.Regiones.ToDictionaryAsync(r => r.Nombre, r => r.Id, cancellationToken);

        foreach (var demo in DemoData.Denominaciones)
        {
            var denominacion = Denominacion.Crear(demo.Nombre, tramos[demo.Tramo], niveles[demo.Nivel].Id);
            context.Denominaciones.Add(denominacion);

            denominacion.DefinirAsignaciones(
                demo.RegionesMacro ?? [],
                (demo.RegionesAsistencia ?? []).Select(r => regiones[r]),
                (demo.TramosAdicionales ?? []).Select(t => tramos[t]));

            var unidad = Unidad.Crear(demo.Ficha, demo.Placa);
            AsignacionDenominacionService.Asignar(unidad, denominacion, [], autor);
            context.Unidades.Add(unidad);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Después, para que las fichas asignadas tomen los primeros Ids
        context.Unidades.AddRange(DemoData.UnidadesEnReserva.Select(u => Unidad.Crear(u.Ficha, u.Placa)));
        await context.SaveChangesAsync(cancellationToken);
        return DemoData.Denominaciones.Length * 2 + DemoData.UnidadesEnReserva.Length;
    }
}
