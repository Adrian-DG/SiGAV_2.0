using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Historico;
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
    int? ColorId);

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

public class GetCiudadanoConocidoQueryHandler(IReadDbContext db) : IRequestHandler<GetCiudadanoConocidoQuery, CiudadanoConocidoViewModel>
{
    public async Task<CiudadanoConocidoViewModel> Handle(GetCiudadanoConocidoQuery request, CancellationToken cancellationToken)
    {
        var identificacion = DatosPersona.NormalizarIdentificacion(request.Identificacion)!;

        return await db.Ciudadanos
            .Where(c => c.Identificacion == identificacion && c.IsActive)
            .Select(c => new CiudadanoConocidoViewModel(identificacion, c.Nombre, c.Apellido, c.Sexo, null, c.NacionalidadId, OrigenDato.Maestro, null))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("La persona", identificacion);
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

public class GetVehiculoConocidoQueryHandler(IReadDbContext db) : IRequestHandler<GetVehiculoConocidoQuery, VehiculoConocidoViewModel>
{
    public async Task<VehiculoConocidoViewModel> Handle(GetVehiculoConocidoQuery request, CancellationToken cancellationToken)
    {
        var placa = DatosVehiculo.NormalizarPlaca(request.Placa)!;

        return await db.Vehiculos
            .Where(x => x.Placa == placa && x.IsActive)
            .Select(x => new VehiculoConocidoViewModel(placa, x.TipoId, x.Modelo != null ? x.Modelo.MarcaId : null, x.ModeloId, x.ColorId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("El vehículo", placa);
    }
}
#endregion
