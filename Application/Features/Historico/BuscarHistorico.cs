using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Historico;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico;

/// <summary>De dónde salieron los datos del histórico para autocompletar.</summary>
public static class OrigenDato
{
    /// <summary>Actualizados por el último evento en que se registró esa cédula/placa.</summary>
    public const string Evento = "evento";

    /// <summary>Importados (p. ej. de SiGAV 1.0): nunca se registró en un evento.</summary>
    public const string Maestro = "maestro";
}

/// <summary>
/// Edad: estimada a hoy con la edad registrada en el último evento. UltimoRegistro: fecha de ese
/// evento (null si los datos son solo importados).
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
/// La persona tal como está en el histórico, que cada evento mantiene al día (lo más reciente gana).
/// </summary>
public class GetCiudadanoConocidoQueryHandler(IReadDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetCiudadanoConocidoQuery, CiudadanoConocidoViewModel>
{
    public async Task<CiudadanoConocidoViewModel> Handle(GetCiudadanoConocidoQuery request, CancellationToken cancellationToken)
    {
        var identificacion = DatosPersona.NormalizarIdentificacion(request.Identificacion)!;

        var ciudadano = await db.Ciudadanos
            .Where(c => c.Identificacion == identificacion && c.IsActive)
            .Select(c => new { c.Nombre, c.Apellido, c.Sexo, c.Telefono, c.NacionalidadId, c.FechaNacimientoEstimada, c.UltimoEventoUtc })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("La persona", identificacion);

        return new CiudadanoConocidoViewModel(
            identificacion,
            ciudadano.Nombre,
            ciudadano.Apellido,
            ciudadano.Sexo,
            ciudadano.Telefono,
            ciudadano.NacionalidadId,
            ciudadano.UltimoEventoUtc is null ? OrigenDato.Maestro : OrigenDato.Evento,
            ciudadano.UltimoEventoUtc,
            Ciudadano.EdadAl(ciudadano.FechaNacimientoEstimada, timeProvider.GetUtcNow().UtcDateTime));
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

/// <summary>El vehículo tal como está en el histórico, que cada evento mantiene al día (lo más reciente gana).</summary>
public class GetVehiculoConocidoQueryHandler(IReadDbContext db) : IRequestHandler<GetVehiculoConocidoQuery, VehiculoConocidoViewModel>
{
    public async Task<VehiculoConocidoViewModel> Handle(GetVehiculoConocidoQuery request, CancellationToken cancellationToken)
    {
        var placa = DatosVehiculo.NormalizarPlaca(request.Placa)!;

        return await db.Vehiculos
            .Where(v => v.Placa == placa && v.IsActive)
            .Select(v => new VehiculoConocidoViewModel(
                placa,
                v.TipoVehiculoId,
                v.MarcaId,
                v.ModeloId,
                v.ColorId,
                v.MarcaTexto,
                v.ModeloTexto,
                v.ColorTexto,
                v.UltimoEventoUtc == null ? OrigenDato.Maestro : OrigenDato.Evento,
                v.UltimoEventoUtc))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("El vehículo", placa);
    }
}
#endregion
