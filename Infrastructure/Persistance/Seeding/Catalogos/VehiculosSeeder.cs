using Domain.Entities.Misc;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Catalogos;

/// <summary>Tipos de vehículo, colores, marcas, modelos y prefijos de placa.</summary>
internal sealed class VehiculosSeeder(SiGAVContext context) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Catalogo;
    public int Orden => 30;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var insertados = 0;
        if (!await context.TipoVehiculos.AnyAsync(cancellationToken))
            insertados += await SembrarVehiculosAsync(cancellationToken);

        // Aparte: una base que ya tenía los tipos de vehículo también recibe los prefijos
        if (!await context.PrefijosPlaca.AnyAsync(cancellationToken))
        {
            var tipoPorNombre = await context.TipoVehiculos.ToDictionaryAsync(t => t.Nombre, cancellationToken);
            context.PrefijosPlaca.AddRange(VehiculosData.PrefijosPlaca.Select(p => new PrefijoPlaca
            {
                Prefijo = p.Prefijo,
                Nombre = p.Nombre,
                Patron = p.Patron,
                Ejemplo = p.Ejemplo,
                TiposVehiculo = p.Tipos.Select(t => tipoPorNombre[t]).ToList(),
                IsActive = true
            }));
            insertados += await context.SaveChangesAsync(cancellationToken);
        }

        return insertados;
    }

    private async Task<int> SembrarVehiculosAsync(CancellationToken cancellationToken)
    {
        var tipos = VehiculosData.TiposVehiculo.Select(nombre => new TipoVehiculo { Nombre = nombre, IsActive = true }).ToList();
        var marcas = VehiculosData.Marcas.Select(nombre => new Marca { Nombre = nombre, IsActive = true }).ToList();

        context.TipoVehiculos.AddRange(tipos);
        context.Colores.AddRange(VehiculosData.Colores.Select(nombre => new Color { Nombre = nombre, IsActive = true }));
        context.Marcas.AddRange(marcas);
        var insertados = await context.SaveChangesAsync(cancellationToken);

        var tipoPorNombre = tipos.ToDictionary(t => t.Nombre);
        var marcaPorNombre = marcas.ToDictionary(m => m.Nombre);
        context.Modelos.AddRange(VehiculosData.Modelos.Select(m => new Modelo
        {
            Nombre = m.Nombre,
            MarcaId = marcaPorNombre[m.Marca].Id,
            TipoVehiculoId = tipoPorNombre[m.Tipo].Id,
            IsActive = true
        }));

        return insertados + await context.SaveChangesAsync(cancellationToken);
    }
}
