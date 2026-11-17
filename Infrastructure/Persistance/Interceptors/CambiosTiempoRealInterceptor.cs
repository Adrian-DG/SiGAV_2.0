using Application.Contracts.Notificaciones;
using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistance.Interceptors;

/// <summary>
/// Detecta, en cada SaveChanges exitoso, qué eventos y unidades cambiaron y lo publica por
/// <see cref="IMonitorTiempoReal"/> (front desk en tiempo real). Centralizarlo aquí evita tener
/// que acordarse de notificar en cada handler: cualquier escritura del agregado Evento (y sus
/// involucrados, unidades, evidencias...) o de una unidad (posición, sesión, denominación) avisa.
/// Las entradas se guardan antes de guardar y los ids se leen después: los registros nuevos
/// reciben su Id (y las FK su valor) recién al insertarse.
/// </summary>
public sealed class CambiosTiempoRealInterceptor(IMonitorTiempoReal monitor, TimeProvider timeProvider, ILogger<CambiosTiempoRealInterceptor> logger)
    : SaveChangesInterceptor
{
    private const string PropiedadEventoId = "EventoId";
    private const string PropiedadUnidadId = "UnidadId";

    private List<EntityEntry> _pendientes = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Recolectar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Recolectar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Publicar();
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Publicar();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _pendientes = [];
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendientes = [];
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Recolectar(DbContext? context)
    {
        if (context is null) return;

        _pendientes = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => EsDeOperaciones(e.Metadata.ClrType))
            .ToList();
    }

    private void Publicar()
    {
        var entradas = _pendientes;
        _pendientes = [];
        if (entradas.Count == 0) return;

        var eventos = new HashSet<int>();
        var unidades = new HashSet<int>();

        foreach (var entry in entradas)
        {
            switch (entry.Entity)
            {
                case Evento evento:
                    eventos.Add(evento.Id);
                    break;
                case Unidad unidad:
                    unidades.Add(unidad.Id);
                    break;
            }

            // Hijos del agregado Evento (unidades, vehículos, ciudadanos, tipos, evidencias)
            if (entry.Entity is not Evento && LeerEntero(entry, PropiedadEventoId) is { } eventoId)
                eventos.Add(eventoId);

            // Posición/sesión, historial de denominación y participación en eventos (disponibilidad)
            if (LeerEntero(entry, PropiedadUnidadId) is { } unidadId)
                unidades.Add(unidadId);
        }

        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        Enviar(TiposCambioTiempoReal.Eventos, eventos, ahoraUtc);
        Enviar(TiposCambioTiempoReal.Unidades, unidades, ahoraUtc);
    }

    private void Enviar(string tipo, HashSet<int> ids, DateTime ahoraUtc)
    {
        ids.RemoveWhere(id => id <= 0);
        if (ids.Count == 0) return;

        // Sin await: el aviso no debe demorar ni hacer fallar la operación que ya se guardó
        _ = EnviarSeguroAsync(new CambioTiempoReal(tipo, ids.ToList(), ahoraUtc));
    }

    private async Task EnviarSeguroAsync(CambioTiempoReal cambio)
    {
        try
        {
            await monitor.PublicarAsync(cambio);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo publicar el cambio en tiempo real {Tipo}", cambio.Tipo);
        }
    }

    private static bool EsDeOperaciones(Type tipo)
        => tipo.Namespace == typeof(Evento).Namespace;

    private static int? LeerEntero(EntityEntry entry, string propiedad)
    {
        if (entry.Metadata.FindProperty(propiedad) is null) return null;

        return entry.Property(propiedad).CurrentValue switch
        {
            int valor => valor,
            _ => null
        };
    }
}
