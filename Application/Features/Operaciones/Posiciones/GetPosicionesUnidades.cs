using Application.Contracts;
using Domain.Entities.Operaciones;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Posiciones;

public record PosicionUnidadViewModel(
    int UnidadId,
    string Ficha,
    string? Placa,
    int? DenominacionId,
    string? Denominacion,
    string? Nivel,
    int? TramoId,
    string? Tramo,
    int AgenteId,
    string Agente,
    string Rango,
    decimal Latitud,
    decimal Longitud,
    double? PrecisionMetros,
    double? Rumbo,
    double? VelocidadKmh,
    DateTime FechaHoraGpsUtc,
    DateTime FechaHoraRecibidaUtc,
    EstadoPosicionEnum Estado);

/// <summary>
/// <see cref="ServidorUtc"/>: hora del servidor al responder, para calcular "hace X min" sin depender
/// del reloj del navegador.
/// </summary>
public record PosicionesUnidadesViewModel(DateTime ServidorUtc, IReadOnlyList<PosicionUnidadViewModel> Unidades);

/// <summary>
/// Última posición de cada unidad activa que haya enviado en las últimas <see cref="VentanaHoras"/>
/// horas (un turno), incluidas las que ya están sin señal o desconectadas.
/// </summary>
public record GetPosicionesUnidadesQuery : IRequest<PosicionesUnidadesViewModel>
{
    public const int VentanaHoras = 24;
}

public class GetPosicionesUnidadesQueryHandler(IReadDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetPosicionesUnidadesQuery, PosicionesUnidadesViewModel>
{
    public async Task<PosicionesUnidadesViewModel> Handle(GetPosicionesUnidadesQuery request, CancellationToken cancellationToken)
    {
        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var desde = ahoraUtc.AddHours(-GetPosicionesUnidadesQuery.VentanaHoras);

        var filas = await db.UnidadPosiciones
            .Where(p => p.FechaHoraRecibidaUtc >= desde && p.Unidad.IsActive)
            .Select(p => new
            {
                p.UnidadId,
                p.Unidad.Ficha,
                p.Unidad.Placa,
                p.Unidad.DenominacionId,
                Denominacion = p.Unidad.Denominacion != null ? p.Unidad.Denominacion.Nombre : null,
                // Nivel de la denominación (Móvil, Grúa, Taller...): el tipo de unidad en el mapa
                Nivel = p.Unidad.Denominacion != null ? p.Unidad.Denominacion.Nivel!.Nombre : null,
                TramoId = p.Unidad.Denominacion != null ? (int?)p.Unidad.Denominacion.TramoId : null,
                Tramo = p.Unidad.Denominacion != null ? p.Unidad.Denominacion.Tramo!.Nombre : null,
                p.AgenteId,
                p.Agente.Nombre,
                p.Agente.Apellido,
                p.Agente.Institucion,
                Rango = p.Agente.Rango != null ? p.Agente.Rango.Nombre : null,
                RangoArmada = p.Agente.Rango != null ? p.Agente.Rango.NombreArmada : null,
                p.Ubicacion.Latitud,
                p.Ubicacion.Longitud,
                p.PrecisionMetros,
                p.Rumbo,
                p.VelocidadKmh,
                p.FechaHoraGpsUtc,
                p.FechaHoraRecibidaUtc,
                p.SesionActiva,
            })
            .ToListAsync(cancellationToken);

        var unidades = filas
            .Select(f => new PosicionUnidadViewModel(
                f.UnidadId, f.Ficha, f.Placa, f.DenominacionId, f.Denominacion, f.Nivel, f.TramoId, f.Tramo,
                f.AgenteId,
                // Igual que Agente.NombreCompleto / GetRango (propiedades calculadas, EF no las traduce)
                $"{f.Apellido} {f.Nombre}",
                (f.Institucion == InstitucionEnum.ARD ? f.RangoArmada : f.Rango) ?? string.Empty,
                f.Latitud, f.Longitud, f.PrecisionMetros, f.Rumbo, f.VelocidadKmh,
                f.FechaHoraGpsUtc, f.FechaHoraRecibidaUtc,
                UnidadPosicion.CalcularEstado(f.SesionActiva, f.FechaHoraRecibidaUtc, ahoraUtc)))
            .OrderBy(u => u.Estado)
            .ThenBy(u => u.Ficha)
            .ToList();

        return new PosicionesUnidadesViewModel(ahoraUtc, unidades);
    }
}
