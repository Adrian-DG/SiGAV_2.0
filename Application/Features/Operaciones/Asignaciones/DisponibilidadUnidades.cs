using Application.Contracts;
using Application.Contracts.Notificaciones;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Asignaciones;

/// <summary>
/// Un agente de la unidad y si puede recibir un evento. <see cref="EventoId"/>: el evento del servidor que
/// atiende; <see cref="OcupadoEnDispositivo"/>: atiende uno que la app aún tiene en el dispositivo.
/// </summary>
public record AgenteDisponibilidadViewModel(
    int AgenteId,
    string Nombre,
    string Rango,
    bool EsComponente,
    bool Disponible,
    int? EventoId,
    bool OcupadoEnDispositivo);

/// <summary>
/// Unidad con sesión abierta en la app. <see cref="Disponible"/>: al menos uno de sus agentes (el de la
/// sesión o su componente) no atiende ningún evento. <see cref="ConectadaNotificaciones"/>: tiene la
/// app abierta y conectada al hub, así que recibirá la notificación al momento.
/// <see cref="EventosActivos"/>: eventos de la unidad sin completar (pendientes o en curso).
/// </summary>
public record UnidadDisponibilidadViewModel(
    int UnidadId,
    string Ficha,
    string? Denominacion,
    string? Tramo,
    EstadoPosicionEnum Estado,
    bool ConectadaNotificaciones,
    decimal Latitud,
    decimal Longitud,
    DateTime FechaHoraRecibidaUtc,
    bool Disponible,
    int EventosActivos,
    IReadOnlyList<AgenteDisponibilidadViewModel> Agentes);

/// <summary>
/// Qué unidades pueden recibir un evento del call center. Una unidad con sesión abierta en la app
/// (la que no está desconectada) está disponible si su agente no atiende ningún evento sin completar
/// o, si opera con componente, si al menos uno de los dos está libre.
///
/// Ocupan al agente los eventos del servidor y los que la app tiene en curso en el dispositivo (aún
/// sin enviar), que reporta con su posición cada ~30 s.
/// </summary>
internal static class DisponibilidadUnidades
{
    public static async Task<List<UnidadDisponibilidadViewModel>> CalcularAsync(
        IReadDbContext db,
        IConexionesUnidades conexiones,
        DateTime ahoraUtc,
        int? unidadId,
        CancellationToken cancellationToken)
    {
        var desde = ahoraUtc - UnidadPosicion.UmbralDesconectada;

        var posiciones = db.UnidadPosiciones.Where(p => p.SesionActiva && p.FechaHoraRecibidaUtc > desde && p.Unidad.IsActive);
        if (unidadId is { } id) posiciones = posiciones.Where(p => p.UnidadId == id);

        var filas = await posiciones
            .Select(p => new
            {
                p.UnidadId,
                p.Unidad.Ficha,
                Denominacion = p.Unidad.Denominacion != null ? p.Unidad.Denominacion.Nombre : null,
                Tramo = p.Unidad.Denominacion != null ? p.Unidad.Denominacion.Tramo!.Nombre : null,
                p.AgenteId,
                p.Agente.Nombre,
                p.Agente.Apellido,
                p.Agente.Institucion,
                Rango = p.Agente.Rango != null ? p.Agente.Rango.Nombre : null,
                RangoArmada = p.Agente.Rango != null ? p.Agente.Rango.NombreArmada : null,
                p.ComponenteId,
                ComponenteNombre = p.Componente != null ? p.Componente.Nombre : null,
                ComponenteApellido = p.Componente != null ? p.Componente.Apellido : null,
                ComponenteInstitucion = p.Componente != null ? (InstitucionEnum?)p.Componente.Institucion : null,
                ComponenteRango = p.Componente != null && p.Componente.Rango != null ? p.Componente.Rango.Nombre : null,
                ComponenteRangoArmada = p.Componente != null && p.Componente.Rango != null ? p.Componente.Rango.NombreArmada : null,
                p.Ubicacion.Latitud,
                p.Ubicacion.Longitud,
                p.FechaHoraRecibidaUtc,
                p.AgenteOcupadoEnDispositivo,
                p.ComponenteOcupadoEnDispositivo,
                p.SesionActiva,
            })
            .ToListAsync(cancellationToken);

        if (filas.Count == 0) return [];

        var unidadIds = filas.Select(f => f.UnidadId).ToList();
        var ocupaciones = await db.EventoUnidades
            .Where(u => unidadIds.Contains(u.UnidadId) && u.Evento!.IsActive && u.Evento.Estado != EstadoEventoEnum.Completado)
            .Select(u => new { u.UnidadId, u.AgenteId, u.EventoId })
            .ToListAsync(cancellationToken);
        var porUnidad = ocupaciones.ToLookup(o => o.UnidadId);

        return filas
            .Select(f =>
            {
                var eventos = porUnidad[f.UnidadId].ToList();
                int? EventoDe(int agenteId) => eventos.Where(e => e.AgenteId == agenteId).Select(e => (int?)e.EventoId).Min();

                var agentes = new List<AgenteDisponibilidadViewModel>
                {
                    Agente(f.AgenteId, $"{f.Apellido} {f.Nombre}", f.Institucion == InstitucionEnum.ARD ? f.RangoArmada : f.Rango, false, EventoDe(f.AgenteId), f.AgenteOcupadoEnDispositivo),
                };
                if (f.ComponenteId is { } componenteId)
                    agentes.Add(Agente(
                        componenteId,
                        $"{f.ComponenteApellido} {f.ComponenteNombre}",
                        f.ComponenteInstitucion == InstitucionEnum.ARD ? f.ComponenteRangoArmada : f.ComponenteRango,
                        true,
                        EventoDe(componenteId),
                        f.ComponenteOcupadoEnDispositivo));

                return new UnidadDisponibilidadViewModel(
                    f.UnidadId, f.Ficha, f.Denominacion, f.Tramo,
                    UnidadPosicion.CalcularEstado(f.SesionActiva, f.FechaHoraRecibidaUtc, ahoraUtc),
                    conexiones.EstaConectada(f.UnidadId),
                    f.Latitud, f.Longitud, f.FechaHoraRecibidaUtc,
                    agentes.Any(a => a.Disponible),
                    eventos.Select(e => e.EventoId).Distinct().Count(),
                    agentes);
            })
            .OrderByDescending(u => u.Disponible)
            .ThenBy(u => u.Estado)
            .ThenBy(u => u.Ficha)
            .ToList();
    }

    private static AgenteDisponibilidadViewModel Agente(int agenteId, string nombre, string? rango, bool esComponente, int? eventoId, bool ocupadoEnDispositivo)
        => new(agenteId, nombre, rango ?? string.Empty, esComponente, eventoId is null && !ocupadoEnDispositivo, eventoId, ocupadoEnDispositivo);
}
