using System.Text.RegularExpressions;
using Application.Contracts.Almacenamiento;

namespace Infrastructure.Almacenamiento;

/// <summary>
/// Archivos en una carpeta del servidor. Para desarrollo y pruebas: en producción se usará un
/// almacenamiento de objetos (Azure Blob Storage o Cloudinary), que no depende del disco de la API.
/// El content type no se guarda aparte: se deduce de la extensión de la clave.
/// </summary>
public sealed partial class AlmacenamientoLocal : IAlmacenamientoArchivos
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private readonly string raiz;

    public AlmacenamientoLocal(string raiz)
    {
        this.raiz = Path.GetFullPath(raiz);
        Directory.CreateDirectory(this.raiz);
    }

    public async Task GuardarAsync(string clave, Stream contenido, string contentType, CancellationToken cancellationToken = default)
    {
        var ruta = Ruta(clave);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);

        // Se escribe a un temporal y se mueve: nunca queda un archivo a medias con el nombre final
        var temporal = ruta + ".tmp";
        await using (var destino = File.Create(temporal))
            await contenido.CopyToAsync(destino, cancellationToken);
        File.Move(temporal, ruta, overwrite: true);
    }

    public Task<ArchivoAlmacenado?> AbrirAsync(string clave, CancellationToken cancellationToken = default)
    {
        var ruta = Ruta(clave);
        if (!File.Exists(ruta)) return Task.FromResult<ArchivoAlmacenado?>(null);

        var stream = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var contentType = ContentTypes.GetValueOrDefault(Path.GetExtension(ruta), "application/octet-stream");
        return Task.FromResult<ArchivoAlmacenado?>(new ArchivoAlmacenado(stream, contentType, stream.Length));
    }

    public Task EliminarAsync(string clave, CancellationToken cancellationToken = default)
    {
        File.Delete(Ruta(clave));
        return Task.CompletedTask;
    }

    /// <summary>Ruta física de la clave. Rechaza claves que podrían salir de la carpeta raíz.</summary>
    private string Ruta(string clave)
    {
        if (!ClaveValida().IsMatch(clave) || clave.Contains(".."))
            throw new ArgumentException($"La clave de almacenamiento '{clave}' no es válida.", nameof(clave));

        var ruta = Path.GetFullPath(Path.Combine(raiz, clave));
        if (!ruta.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"La clave de almacenamiento '{clave}' no es válida.", nameof(clave));
        return ruta;
    }

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9/_.-]*$")]
    private static partial Regex ClaveValida();
}
