using System.Security.Cryptography;
using System.Text.Json;
using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalogos;

public record MunicipioItemViewModel(int Id, string Nombre, int ProvinciaId);

public record ModeloItemViewModel(int Id, string Nombre, int MarcaId, int TipoVehiculoId);

/// <summary>Todos los catálogos activos que la app guarda en el dispositivo para trabajar sin conexión.</summary>
public record CatalogosMovilViewModel(
    IReadOnlyList<CatalogoItemViewModel> Provincias,
    IReadOnlyList<MunicipioItemViewModel> Municipios,
    IReadOnlyList<TipoEventoItemViewModel> TiposEvento,
    IReadOnlyList<CatalogoItemViewModel> TiposCierre,
    IReadOnlyList<CatalogoItemViewModel> Nacionalidades,
    IReadOnlyList<CatalogoItemViewModel> Colores,
    IReadOnlyList<CatalogoItemViewModel> TiposVehiculo,
    IReadOnlyList<CatalogoItemViewModel> Marcas,
    IReadOnlyList<ModeloItemViewModel> Modelos);

/// <summary>
/// <see cref="Version"/> identifica el contenido (hash). <see cref="Catalogos"/> es null cuando la
/// app ya tiene esa versión: no hace falta volver a descargar ni reemplazar nada.
/// </summary>
public record CatalogosMovilResult(string Version, CatalogosMovilViewModel? Catalogos);

/// <summary>
/// Paquete de catálogos para la app móvil. <paramref name="VersionActual"/> es la versión que el
/// dispositivo ya tiene guardada (si tiene alguna).
/// </summary>
public record GetCatalogosMovilQuery(string? VersionActual = null) : IRequest<CatalogosMovilResult>;

public class GetCatalogosMovilQueryHandler(IReadDbContext db) : IRequestHandler<GetCatalogosMovilQuery, CatalogosMovilResult>
{
    private static readonly JsonSerializerOptions HashJsonOptions = new() { WriteIndented = false };

    public async Task<CatalogosMovilResult> Handle(GetCatalogosMovilQuery request, CancellationToken cancellationToken)
    {
        // Orden estable (por Id): el hash solo debe cambiar si cambia el contenido
        var catalogos = new CatalogosMovilViewModel(
            await db.Provincias.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre)).ToListAsync(cancellationToken),
            await db.Municipios.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new MunicipioItemViewModel(x.Id, x.Nombre, x.ProvinciaId)).ToListAsync(cancellationToken),
            await db.TiposEvento.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new TipoEventoItemViewModel(x.Id, x.Nombre, x.Categoria)).ToListAsync(cancellationToken),
            await db.TiposCierre.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre)).ToListAsync(cancellationToken),
            await db.Nacionalidades.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre)).ToListAsync(cancellationToken),
            await db.Colores.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre)).ToListAsync(cancellationToken),
            await db.TiposVehiculo.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre)).ToListAsync(cancellationToken),
            await db.Marcas.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new CatalogoItemViewModel(x.Id, x.Nombre)).ToListAsync(cancellationToken),
            await db.Modelos.Where(x => x.IsActive).OrderBy(x => x.Id)
                .Select(x => new ModeloItemViewModel(x.Id, x.Nombre, x.MarcaId, x.TipoVehiculoId)).ToListAsync(cancellationToken));

        var version = CalcularVersion(catalogos);
        return string.Equals(version, request.VersionActual, StringComparison.Ordinal)
            ? new CatalogosMovilResult(version, null)
            : new CatalogosMovilResult(version, catalogos);
    }

    private static string CalcularVersion(CatalogosMovilViewModel catalogos)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(catalogos, HashJsonOptions);
        return Convert.ToHexString(SHA256.HashData(json))[..16].ToLowerInvariant();
    }
}
