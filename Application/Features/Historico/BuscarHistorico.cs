using Application.Contracts.Historico;
using Application.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Historico;

/// <summary>De dónde salió el dato para autocompletar.</summary>
public static class OrigenDato
{
    /// <summary>El último evento en que se registró esa cédula/placa (lo más reciente).</summary>
    public const string Evento = "evento";

    /// <summary>Maestro histórico (p. ej. importado de SiGAV 1.0), si nunca se registró en un evento.</summary>
    public const string Maestro = "maestro";
}

public record CiudadanoConocidoViewModel(
    string Identificacion,
    string? Nombre,
    string? Apellido,
    SexoEnum Sexo,
    string? Telefono,
    int? NacionalidadId,
    string Origen,
    DateTime? UltimoRegistro);

public record VehiculoConocidoViewModel(
    string Placa,
    int? TipoVehiculoId,
    int? MarcaId,
    int? ModeloId,
    int? ColorId,
    string? MarcaTexto,
    string? ModeloTexto,
    string? ColorTexto,
    string Origen,
    DateTime? UltimoRegistro);

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

public class GetCiudadanoConocidoQueryHandler(IHistoricoQueries queries) : IRequestHandler<GetCiudadanoConocidoQuery, CiudadanoConocidoViewModel>
{
    public async Task<CiudadanoConocidoViewModel> Handle(GetCiudadanoConocidoQuery request, CancellationToken cancellationToken)
    {
        var identificacion = DatosPersona.NormalizarIdentificacion(request.Identificacion)!;
        return await queries.BuscarCiudadanoAsync(identificacion, cancellationToken)
            ?? throw new NotFoundException("La persona", identificacion);
    }
}

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

public class GetVehiculoConocidoQueryHandler(IHistoricoQueries queries) : IRequestHandler<GetVehiculoConocidoQuery, VehiculoConocidoViewModel>
{
    public async Task<VehiculoConocidoViewModel> Handle(GetVehiculoConocidoQuery request, CancellationToken cancellationToken)
    {
        var placa = DatosVehiculo.NormalizarPlaca(request.Placa)!;
        return await queries.BuscarVehiculoAsync(placa, cancellationToken)
            ?? throw new NotFoundException("El vehículo", placa);
    }
}
