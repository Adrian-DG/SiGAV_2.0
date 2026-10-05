using Application.Contracts;
using Application.Contracts.Almacenamiento;
using Application.Exceptions;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Features.Operaciones.Eventos;

/// <summary>Formatos y tamaño admitidos para las evidencias (fotos y firmas).</summary>
public static class ArchivoEvidencia
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public const string Campo = "Archivo";

    /// <summary>
    /// Formato real del archivo según sus primeros bytes (no se confía en el nombre ni en el
    /// content type que declara el cliente). null si no es JPEG, PNG ni WebP.
    /// </summary>
    public static (string ContentType, string Extension)? DetectarFormato(ReadOnlySpan<byte> inicio)
    {
        if (inicio.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return ("image/jpeg", ".jpg");
        if (inicio.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return ("image/png", ".png");
        if (inicio.Length >= 12 && inicio[..4].SequenceEqual("RIFF"u8) && inicio[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", ".webp");
        return null;
    }

    internal static ValidationException Error(string mensaje) => new([new ValidationFailure(Campo, mensaje)]);
}

public record RegistrarEvidenciaResult(int Id, bool EsDuplicado);

/// <summary>
/// Sube una foto o firma al almacenamiento y la asocia al evento. Idempotente por
/// <see cref="RequestId"/> (cola offline de la app): un reenvío devuelve la evidencia ya registrada
/// con EsDuplicado = true, sin subir el archivo otra vez. Persona o vehículo: ids de este evento
/// (EventoCiudadanoViewModel.Id / EventoVehiculoViewModel.Id).
/// </summary>
public record RegistrarEvidenciaCommand(
    int EventoId,
    TipoEvidenciaEnum Tipo,
    Stream Contenido,
    Guid? RequestId = null,
    int? EventoCiudadanoId = null,
    int? EventoVehiculoId = null) : IRequest<RegistrarEvidenciaResult>;

public class RegistrarEvidenciaCommandValidator : AbstractValidator<RegistrarEvidenciaCommand>
{
    public RegistrarEvidenciaCommandValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum().WithMessage("El tipo de evidencia no es válido.");
        RuleFor(x => x.Contenido).NotNull().WithName(ArchivoEvidencia.Campo).WithMessage("El archivo es requerido.");
        RuleFor(x => x.RequestId).NotEqual(Guid.Empty).WithMessage("El requestId no es válido.");
        RuleFor(x => x.EventoCiudadanoId).GreaterThan(0).When(x => x.EventoCiudadanoId.HasValue)
            .WithMessage("La persona indicada no es válida.");
        RuleFor(x => x.EventoVehiculoId).GreaterThan(0).When(x => x.EventoVehiculoId.HasValue)
            .WithMessage("El vehículo indicado no es válido.");
    }
}

public class RegistrarEvidenciaCommandHandler(
    IEventoRepository eventos,
    IAlmacenamientoArchivos almacenamiento,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<RegistrarEvidenciaCommand, RegistrarEvidenciaResult>
{
    public async Task<RegistrarEvidenciaResult> Handle(RegistrarEvidenciaCommand request, CancellationToken cancellationToken)
    {
        var evento = await eventos.GetParaEvidenciasAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        EventoAcceso.AsegurarParticipa(evento, currentUser);

        if (request.RequestId is { } requestId && evento.Evidencias.FirstOrDefault(e => e.RequestId == requestId) is { } existente)
            return new RegistrarEvidenciaResult(existente.Id, EsDuplicado: true);

        await using var archivo = await LeerAsync(request.Contenido, cancellationToken);
        var formato = ArchivoEvidencia.DetectarFormato(archivo.GetBuffer().AsSpan(0, (int)Math.Min(archivo.Length, 16)))
            ?? throw ArchivoEvidencia.Error("El archivo debe ser una imagen JPEG, PNG o WebP.");

        // El dominio valida antes de subir nada (evento activo, persona/vehículo del evento, límite)
        var clave = $"eventos/{evento.Id}/{Guid.NewGuid():N}{formato.Extension}";
        var evidencia = evento.AgregarEvidencia(
            request.Tipo,
            clave,
            formato.ContentType,
            archivo.Length,
            timeProvider.GetUtcNow().UtcDateTime,
            request.RequestId,
            request.EventoCiudadanoId,
            request.EventoVehiculoId);

        await almacenamiento.GuardarAsync(clave, archivo, formato.ContentType, cancellationToken);
        try
        {
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Sin fila que lo referencie el archivo quedaría huérfano
            await almacenamiento.EliminarAsync(clave, CancellationToken.None);
            throw;
        }

        return new RegistrarEvidenciaResult(evidencia.Id, EsDuplicado: false);
    }

    /// <summary>Copia el archivo a memoria (como máximo <see cref="ArchivoEvidencia.MaxBytes"/>).</summary>
    private static async Task<MemoryStream> LeerAsync(Stream contenido, CancellationToken cancellationToken)
    {
        var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int leidos;
        while ((leidos = await contenido.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memoria.Length + leidos > ArchivoEvidencia.MaxBytes)
                throw ArchivoEvidencia.Error($"El archivo no puede exceder {ArchivoEvidencia.MaxBytes / (1024 * 1024)} MB.");
            memoria.Write(buffer, 0, leidos);
        }

        if (memoria.Length == 0) throw ArchivoEvidencia.Error("El archivo está vacío.");
        memoria.Position = 0;
        return memoria;
    }
}

public record ArchivoEvidenciaResult(Stream Contenido, string ContentType, string NombreArchivo);

/// <summary>Archivo de una evidencia. Para la app, solo de eventos en que participa su unidad (si no, 404).</summary>
public record GetArchivoEvidenciaQuery(int EventoId, int EvidenciaId) : IRequest<ArchivoEvidenciaResult>;

public class GetArchivoEvidenciaQueryHandler(
    IReadDbContext db,
    IAlmacenamientoArchivos almacenamiento,
    ICurrentUserService currentUser) : IRequestHandler<GetArchivoEvidenciaQuery, ArchivoEvidenciaResult>
{
    public async Task<ArchivoEvidenciaResult> Handle(GetArchivoEvidenciaQuery request, CancellationToken cancellationToken)
    {
        var eventosVisibles = db.Eventos.Where(e => e.Id == request.EventoId);
        if (EventoAcceso.UnidadDeAlcance(currentUser) is { } unidadId)
            eventosVisibles = eventosVisibles.Where(e => e.Unidades.Any(u => u.UnidadId == unidadId));

        var evidencia = await eventosVisibles
            .SelectMany(e => e.Evidencias)
            .Where(ev => ev.Id == request.EvidenciaId)
            .Select(ev => new { ev.Ubicacion, ev.ContentType })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("La evidencia", request.EvidenciaId);

        var archivo = await almacenamiento.AbrirAsync(evidencia.Ubicacion, cancellationToken)
            ?? throw new NotFoundException("El archivo de la evidencia", request.EvidenciaId);

        return new ArchivoEvidenciaResult(
            archivo.Contenido,
            evidencia.ContentType,
            $"evento-{request.EventoId}-evidencia-{request.EvidenciaId}{Path.GetExtension(evidencia.Ubicacion)}");
    }
}
