namespace Application.Contracts.Almacenamiento;

/// <summary>
/// Almacenamiento de archivos (evidencias de eventos) fuera de la base de datos. La aplicación solo
/// conoce claves relativas como "eventos/15/3f2a….jpg"; cada proveedor las traduce a lo suyo
/// (Local: ruta bajo una carpeta; Azure Blob: nombre del blob en un contenedor; Cloudinary:
/// public_id). Así cambiar de proveedor no toca la base ni las Queries.
/// Implementaciones en Infrastructure/Almacenamiento; se elige con "Almacenamiento:Proveedor".
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>Guarda (o reemplaza) el archivo con esa clave.</summary>
    Task GuardarAsync(string clave, Stream contenido, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Abre el archivo para leerlo, o null si no existe. El llamador libera el stream.</summary>
    Task<ArchivoAlmacenado?> AbrirAsync(string clave, CancellationToken cancellationToken = default);

    /// <summary>Elimina el archivo; no falla si ya no existe.</summary>
    Task EliminarAsync(string clave, CancellationToken cancellationToken = default);
}

public sealed record ArchivoAlmacenado(Stream Contenido, string ContentType, long? TamanoBytes);
