using Application.Contracts;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Nombres de tipo, marca, modelo y color de los vehículos de un resultado, cargados en un solo
/// viaje por catálogo (en el evento el vehículo solo guarda Ids o texto libre).
/// </summary>
internal sealed record CatalogoVehiculo(
    Dictionary<int, string> Tipos,
    Dictionary<int, string> Marcas,
    Dictionary<int, string> Modelos,
    Dictionary<int, string> Colores)
{
    public static async Task<CatalogoVehiculo> CargarAsync(IReadDbContext db, IEnumerable<DatosVehiculo?> vehiculos, CancellationToken cancellationToken)
    {
        var lista = vehiculos.OfType<DatosVehiculo>().ToList();
        var tipoIds = lista.Select(v => v.TipoVehiculoId).OfType<int>().Distinct().ToList();
        var marcaIds = lista.Select(v => v.MarcaId).OfType<int>().Distinct().ToList();
        var modeloIds = lista.Select(v => v.ModeloId).OfType<int>().Distinct().ToList();
        var colorIds = lista.Select(v => v.ColorId).OfType<int>().Distinct().ToList();

        return new CatalogoVehiculo(
            tipoIds.Count == 0 ? [] : await db.TiposVehiculo.Where(x => tipoIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken),
            marcaIds.Count == 0 ? [] : await db.Marcas.Where(x => marcaIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken),
            modeloIds.Count == 0 ? [] : await db.Modelos.Where(x => modeloIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken),
            colorIds.Count == 0 ? [] : await db.Colores.Where(x => colorIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken));
    }

    /// <summary>Nombre de catálogo o, si el dato no estaba en el catálogo, el texto libre.</summary>
    public EventoVehiculoViewModel Describir(DatosVehiculo v)
    {
        var tipo = v.TipoVehiculoId is { } t ? Tipos.GetValueOrDefault(t) : null;
        var marca = v.MarcaId is { } m ? Marcas.GetValueOrDefault(m) : v.MarcaTexto;
        var modelo = v.ModeloId is { } mo ? Modelos.GetValueOrDefault(mo) : v.ModeloTexto;
        var color = v.ColorId is { } c ? Colores.GetValueOrDefault(c) : v.ColorTexto;

        var marcaModelo = string.Join(' ', new[] { marca, modelo }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var partes = new[] { marcaModelo.Length > 0 ? marcaModelo : tipo, color, v.Placa ?? "Sin placa" }
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return new EventoVehiculoViewModel(v.Placa, tipo, marca, modelo, color, string.Join(" · ", partes));
    }
}
