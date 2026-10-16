using System.Collections.Concurrent;
using Application.Contracts.Notificaciones;

namespace Presentation.Hubs;

/// <summary>
/// Conexiones abiertas al hub por unidad (una unidad puede tener varias: reconexiones, dos
/// dispositivos). Vive en memoria del proceso: con más de una instancia de la API haría falta un
/// backplane (Redis) y guardar esto fuera del proceso.
/// </summary>
public sealed class ConexionesUnidades : IConexionesUnidades
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> _porUnidad = new();

    public void Agregar(int unidadId, string connectionId)
        => _porUnidad.GetOrAdd(unidadId, _ => new()).TryAdd(connectionId, 0);

    public void Quitar(int unidadId, string connectionId)
    {
        if (!_porUnidad.TryGetValue(unidadId, out var conexiones)) return;

        conexiones.TryRemove(connectionId, out _);
        // Solo se quita la unidad si sigue vacía (otra conexión pudo llegar entre medio)
        if (conexiones.IsEmpty)
            _porUnidad.TryRemove(new KeyValuePair<int, ConcurrentDictionary<string, byte>>(unidadId, conexiones));
    }

    public bool EstaConectada(int unidadId)
        => _porUnidad.TryGetValue(unidadId, out var conexiones) && !conexiones.IsEmpty;

    public IReadOnlyCollection<int> Conectadas()
        => _porUnidad.Where(p => !p.Value.IsEmpty).Select(p => p.Key).ToList();
}
