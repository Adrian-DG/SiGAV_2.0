using Domain.Entities.Historico;
using Domain.ValueObjects;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Demo;

/// <summary>
/// Maestros de ciudadanos y vehículos, para probar el autocompletado por cédula y placa del
/// formulario de eventos (en producción vienen del legacy).
/// </summary>
internal sealed class HistoricoDemoSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Demo;
    public int Orden => 220;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (!await context.Ciudadanos.AnyAsync(cancellationToken))
        {
            var nacionalidades = await context.Nacionalidades.ToDictionaryAsync(n => n.Nombre, n => n.Id, cancellationToken);

            context.Ciudadanos.AddRange(DemoData.Ciudadanos.Select(c => new Ciudadano
            {
                // Mismo formato con el que se buscan (sin guiones ni espacios)
                Identificacion = DatosPersona.NormalizarIdentificacion(c.Identificacion)!,
                Nombre = c.Nombre,
                Apellido = c.Apellido,
                Sexo = c.Sexo,
                NacionalidadId = nacionalidades[c.Nacionalidad],
                IsActive = true
            }));
        }

        if (!await context.Vehiculos.AnyAsync(cancellationToken))
        {
            var tipos = await context.TipoVehiculos.ToDictionaryAsync(t => t.Nombre, t => t.Id, cancellationToken);
            var colores = await context.Colores.ToDictionaryAsync(c => c.Nombre, c => c.Id, cancellationToken);
            var modelos = await context.Modelos
                .Select(m => new { m.Id, m.Nombre, Marca = m.Marca!.Nombre })
                .ToDictionaryAsync(m => (m.Marca, m.Nombre), m => m.Id, cancellationToken);

            context.Vehiculos.AddRange(DemoData.Vehiculos.Select(v => new Vehiculo
            {
                Placa = DatosVehiculo.NormalizarPlaca(v.Placa)!,
                TipoId = tipos[v.Tipo],
                ColorId = colores[v.Color],
                ModeloId = modelos[(v.Marca, v.Modelo)],
                Fabricacion = v.Fabricacion,
                IsActive = true
            }));
        }

        return await context.SaveChangesAsync(cancellationToken);
    }
}
