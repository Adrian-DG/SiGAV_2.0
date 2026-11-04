using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Calidad;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Calidad;

// Los comandos de la web registran siempre por llamada telefónica y con el usuario de la sesión
// (nunca uno enviado en el cuerpo). Un futuro bot de mensajería usará los mismos métodos del
// dominio con CanalContactoEnum.WhatsApp y usuario null.

// ---------------------------------------------------------------- Iniciar

/// <summary>
/// Inicia la evaluación del evento con la encuesta vigente. Contacto: una persona del evento (su
/// teléfono si no se indica otro) o, sin persona, el teléfono y nombre indicados.
/// </summary>
public record IniciarEvaluacionCommand(int EventoId, int? EventoCiudadanoId, string? Telefono, string? NombreContacto) : IRequest<int>;

public class IniciarEvaluacionCommandValidator : AbstractValidator<IniciarEvaluacionCommand>
{
    public IniciarEvaluacionCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.EventoCiudadanoId is not null || !string.IsNullOrWhiteSpace(x.Telefono))
            .WithMessage("Elija la persona a contactar o indique un teléfono.");
    }
}

public class IniciarEvaluacionCommandHandler(
    IEventoRepository eventos,
    IEncuestaRepository encuestas,
    IEvaluacionCalidadRepository evaluaciones,
    IUnitOfWork uow) : IRequestHandler<IniciarEvaluacionCommand, int>
{
    public async Task<int> Handle(IniciarEvaluacionCommand request, CancellationToken cancellationToken)
    {
        if (await evaluaciones.ExisteParaEventoAsync(request.EventoId, cancellationToken))
            throw new ConflictException("El evento ya tiene una evaluación de calidad.");

        var evento = await eventos.GetConPersonasAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        var encuesta = await encuestas.GetVigenteAsync(cancellationToken)
            ?? throw new ConflictException("No hay una encuesta de calidad publicada.");

        var evaluacion = EvaluacionCalidad.Iniciar(
            evento, encuesta, CanalContactoEnum.Llamada, request.EventoCiudadanoId, request.Telefono, request.NombreContacto);
        evaluaciones.Add(evaluacion);

        await uow.SaveChangesAsync(cancellationToken);
        return evaluacion.Id;
    }
}

// ---------------------------------------------------------------- Contacto

public record CambiarContactoEvaluacionCommand(int EventoId, int? EventoCiudadanoId, string? Telefono, string? NombreContacto) : IRequest;

public class CambiarContactoEvaluacionCommandHandler(IEvaluacionCalidadRepository evaluaciones, IUnitOfWork uow)
    : IRequestHandler<CambiarContactoEvaluacionCommand, Unit>
{
    public async Task<Unit> Handle(CambiarContactoEvaluacionCommand request, CancellationToken cancellationToken)
    {
        var evaluacion = await EvaluacionCalidadCarga.ObtenerAsync(evaluaciones, request.EventoId, cancellationToken);
        evaluacion.CambiarContacto(evaluacion.Evento!, request.EventoCiudadanoId, request.Telefono, request.NombreContacto);
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------- Intentos

public record RegistrarIntentoContactoCommand(int EventoId, ResultadoContactoEnum Resultado, string? Observacion) : IRequest;

public class RegistrarIntentoContactoCommandValidator : AbstractValidator<RegistrarIntentoContactoCommand>
{
    public RegistrarIntentoContactoCommandValidator()
    {
        RuleFor(x => x.Resultado)
            .Must(r => r is not (ResultadoContactoEnum.MensajeEnviado or ResultadoContactoEnum.SinRespuesta) && Enum.IsDefined(r))
            .WithMessage("El resultado de la llamada no es válido.");
        RuleFor(x => x.Observacion).MaximumLength(IntentoContacto.ObservacionMaxLength);
    }
}

public class RegistrarIntentoContactoCommandHandler(
    IEvaluacionCalidadRepository evaluaciones,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<RegistrarIntentoContactoCommand, Unit>
{
    public async Task<Unit> Handle(RegistrarIntentoContactoCommand request, CancellationToken cancellationToken)
    {
        var evaluacion = await EvaluacionCalidadCarga.ObtenerAsync(evaluaciones, request.EventoId, cancellationToken);
        evaluacion.RegistrarIntento(
            CanalContactoEnum.Llamada,
            request.Resultado,
            currentUser.UserId,
            request.Observacion,
            timeProvider.GetUtcNow().UtcDateTime);
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------- Respuestas

public record RespuestaInput(int PreguntaId, int? Valor, string? Texto, string? Comentario);

/// <summary>
/// Guarda las respuestas indicadas (una pregunta sin valor ni texto queda sin responder). Con
/// <see cref="Completar"/> además cierra la evaluación: exige todas las obligatorias.
/// </summary>
public record GuardarRespuestasEvaluacionCommand(
    int EventoId,
    IReadOnlyList<RespuestaInput> Respuestas,
    bool Completar = false,
    string? Observacion = null) : IRequest;

public class GuardarRespuestasEvaluacionCommandValidator : AbstractValidator<GuardarRespuestasEvaluacionCommand>
{
    public GuardarRespuestasEvaluacionCommandValidator()
    {
        RuleFor(x => x.Respuestas).NotNull().WithMessage("Indique las respuestas.");
        RuleFor(x => x.Respuestas)
            .Must(r => r.Select(x => x.PreguntaId).Distinct().Count() == r.Count)
            .When(x => x.Respuestas is not null)
            .WithMessage("Cada pregunta puede responderse una sola vez.");
        RuleFor(x => x.Observacion).MaximumLength(EvaluacionCalidad.ObservacionMaxLength);
    }
}

public class GuardarRespuestasEvaluacionCommandHandler(
    IEvaluacionCalidadRepository evaluaciones,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<GuardarRespuestasEvaluacionCommand, Unit>
{
    public async Task<Unit> Handle(GuardarRespuestasEvaluacionCommand request, CancellationToken cancellationToken)
    {
        var evaluacion = await EvaluacionCalidadCarga.ObtenerAsync(evaluaciones, request.EventoId, cancellationToken);

        foreach (var respuesta in request.Respuestas)
            evaluacion.Responder(respuesta.PreguntaId, respuesta.Valor, respuesta.Texto, respuesta.Comentario);

        if (request.Completar)
            evaluacion.Completar(currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime, request.Observacion);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------- Cierre sin encuesta / reapertura

public record MarcarNoContactadoCommand(int EventoId, string? Observacion) : IRequest;

public class MarcarNoContactadoCommandHandler(
    IEvaluacionCalidadRepository evaluaciones,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<MarcarNoContactadoCommand, Unit>
{
    public async Task<Unit> Handle(MarcarNoContactadoCommand request, CancellationToken cancellationToken)
    {
        var evaluacion = await EvaluacionCalidadCarga.ObtenerAsync(evaluaciones, request.EventoId, cancellationToken);
        evaluacion.MarcarNoContactado(currentUser.UserId, timeProvider.GetUtcNow().UtcDateTime, request.Observacion);
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record ReabrirEvaluacionCommand(int EventoId) : IRequest;

public class ReabrirEvaluacionCommandHandler(IEvaluacionCalidadRepository evaluaciones, IUnitOfWork uow)
    : IRequestHandler<ReabrirEvaluacionCommand, Unit>
{
    public async Task<Unit> Handle(ReabrirEvaluacionCommand request, CancellationToken cancellationToken)
    {
        var evaluacion = await EvaluacionCalidadCarga.ObtenerAsync(evaluaciones, request.EventoId, cancellationToken);
        evaluacion.Reabrir();
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

internal static class EvaluacionCalidadCarga
{
    public static async Task<EvaluacionCalidad> ObtenerAsync(IEvaluacionCalidadRepository evaluaciones, int eventoId, CancellationToken cancellationToken)
        => await evaluaciones.GetByEventoIdAsync(eventoId, cancellationToken)
           ?? throw new NotFoundException("La evaluación del evento", eventoId);
}
