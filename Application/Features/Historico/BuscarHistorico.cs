using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico;

/// <summary>De dónde salió el dato para autocompletar.</summary>
public static class OrigenDato
{
    /// <summary>El último evento en que se registró esa cédula/placa (lo más reciente).</summary>
    public const string Evento = "evento";

    /// <summary>Maestro histórico (p. ej. importado de SiGAV 1.0), si nunca se registró en un evento.</summary>
    public const string Maestro = "maestro";
}

/// <summary>
/// Edad: la registrada en el último evento más los años transcurridos desde entonces (estimada; el
/// maestro no guarda fecha de nacimiento). UltimoRegistro: fecha de ese evento (null si viene del maestro).
/// </summary>
public record CiudadanoConocidoViewModel(
    string Identificacion,
    string? Nombre,
    string? Apellido,
    SexoEnum Sexo,
    string? Telefono,
    int? NacionalidadId,
    string Origen,
    DateTime? UltimoRegistro,
    int? Edad = null);

/// <summary>*Texto: marca/modelo/color escritos a mano cuando no estaban en el catálogo.</summary>
public record VehiculoConocidoViewModel(
    string Placa,
    int? TipoVehiculoId,
    int? MarcaId,
    int? ModeloId,
    int? ColorId,
    string? MarcaTexto = null,
    string? ModeloTexto = null,
    string? ColorTexto = null,
    string Origen = OrigenDato.Maestro,
    DateTime? UltimoRegistro = null);

#region Ciudadano

// Query: datos conocidos de una persona para autocompletar el formulario de evento
public record GetCiudadanoConocidoQuery(string Identificacion) : IRequest<CiudadanoConocidoViewModel>;

public class GetCiudadanoConocidoQueryValidator : AbstractValidator<GetCiudadanoConocidoQuery>
{
    public GetCiudadanoConocidoQueryValidator()
    {
        RuleFor(x => x.Identificacion)
            .Must(i => DatosPersonaValida(i)).WithMessage("La cédula o pasaporte no es válida.");
    }

    private static bool DatosPersonaValida(string? identificacion)
    {
        try { return DatosPersona.NormalizarIdentificacion(identificacion) is not null; }
        catch (Domain.Exceptions.DomainException) { return false; }
    }
}

/// <summary>
/// Lo más reciente gana: la persona tal como se registró en su último evento; lo que ese registro
/// no trae (p. ej. el nombre de alguien registrado solo con cédula) se completa con el maestro.
/// </summary>
public class GetCiudadanoConocidoQueryHandler(IReadDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetCiudadanoConocidoQuery, CiudadanoConocidoViewModel>
{
    public async Task<CiudadanoConocidoViewModel> Handle(GetCiudadanoConocidoQuery request, CancellationToken cancellationToken)
    {
        var identificacion = DatosPersona.NormalizarIdentificacion(request.Identificacion)!;

        var evento = await db.EventoCiudadanos
            .Where(c => c.Persona.Identificacion == identificacion && c.Evento!.IsActive)
            .OrderByDescending(c => c.Evento!.FechaHoraReporteUtc)
            .ThenByDescending(c => c.Id)
            .Select(c => new
            {
                c.Persona.Nombre,
                c.Persona.Apellido,
                c.Persona.Sexo,
                c.Persona.Telefono,
                c.Persona.NacionalidadId,
                c.Persona.Edad,
                Fecha = c.Evento!.FechaHoraReporteUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        var maestro = await db.Ciudadanos
            .Where(c => c.Identificacion == identificacion && c.IsActive)
            .Select(c => new { c.Nombre, c.Apellido, c.Sexo, c.NacionalidadId })
            .FirstOrDefaultAsync(cancellationToken);

        if (evento is not null)
            return new CiudadanoConocidoViewModel(
                identificacion,
                evento.Nombre ?? maestro?.Nombre,
                evento.Apellido ?? maestro?.Apellido,
                evento.Sexo != SexoEnum.NONE ? evento.Sexo : maestro?.Sexo ?? SexoEnum.NONE,
                evento.Telefono,
                evento.NacionalidadId ?? maestro?.NacionalidadId,
                OrigenDato.Evento,
                evento.Fecha,
                EdadActual(evento.Edad, evento.Fecha));

        if (maestro is not null)
            return new CiudadanoConocidoViewModel(
                identificacion, maestro.Nombre, maestro.Apellido, maestro.Sexo, null, maestro.NacionalidadId, OrigenDato.Maestro, null);

        throw new NotFoundException("La persona", identificacion);
    }

    /// <summary>La edad registrada más los años cumplidos desde aquel evento.</summary>
    private int? EdadActual(int? edad, DateTime fechaUtc)
    {
        if (edad is not { } e) return null;
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var anios = ahora.Year - fechaUtc.Year - (ahora.DayOfYear < fechaUtc.DayOfYear ? 1 : 0);
        return Math.Min(e + Math.Max(anios, 0), DatosPersona.EdadMaxima);
    }
}

#endregion

#region Vehiculo
// Query: datos conocidos de un vehículo para autocompletar el formulario de evento
public record GetVehiculoConocidoQuery(string Placa) : IRequest<VehiculoConocidoViewModel>;

public class GetVehiculoConocidoQueryValidator : AbstractValidator<GetVehiculoConocidoQuery>
{
    public GetVehiculoConocidoQueryValidator()
    {
        RuleFor(x => x.Placa).Must(p => PlacaValida(p)).WithMessage("La placa no es válida.");
    }

    private static bool PlacaValida(string? placa)
    {
        try { return DatosVehiculo.NormalizarPlaca(placa) is not null; }
        catch (Domain.Exceptions.DomainException) { return false; }
    }
}

/// <summary>El vehículo tal como se registró en su último evento; si nunca se registró, el maestro.</summary>
public class GetVehiculoConocidoQueryHandler(IReadDbContext db) : IRequestHandler<GetVehiculoConocidoQuery, VehiculoConocidoViewModel>
{
    public async Task<VehiculoConocidoViewModel> Handle(GetVehiculoConocidoQuery request, CancellationToken cancellationToken)
    {
        var placa = DatosVehiculo.NormalizarPlaca(request.Placa)!;

        var evento = await db.EventoVehiculos
            .Where(v => v.Datos.Placa == placa && v.Evento!.IsActive)
            .OrderByDescending(v => v.Evento!.FechaHoraReporteUtc)
            .ThenByDescending(v => v.Id)
            .Select(v => new VehiculoConocidoViewModel(
                placa,
                v.Datos.TipoVehiculoId,
                v.Datos.MarcaId,
                v.Datos.ModeloId,
                v.Datos.ColorId,
                v.Datos.MarcaTexto,
                v.Datos.ModeloTexto,
                v.Datos.ColorTexto,
                OrigenDato.Evento,
                v.Evento!.FechaHoraReporteUtc))
            .FirstOrDefaultAsync(cancellationToken);
        if (evento is not null) return evento;

        return await db.Vehiculos
            .Where(x => x.Placa == placa && x.IsActive)
            .Select(x => new VehiculoConocidoViewModel(placa, x.TipoId, x.Modelo != null ? x.Modelo.MarcaId : null, x.ModeloId, x.ColorId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("El vehículo", placa);
    }
}
#endregion
