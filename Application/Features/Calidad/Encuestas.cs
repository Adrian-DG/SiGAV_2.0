using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Calidad;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Calidad;

// Versiones de la encuesta de calidad. Cambiar preguntas = nueva versión (borrador copiado de la
// vigente) → editar → publicar; la anterior queda retirada y sus evaluaciones la conservan.

// ---------------------------------------------------------------- Consultas

/// <summary>Todas las versiones, la más reciente primero.</summary>
public record GetEncuestasQuery : IRequest<IReadOnlyList<EncuestaListItemViewModel>>;

public class GetEncuestasQueryHandler(IReadDbContext db) : IRequestHandler<GetEncuestasQuery, IReadOnlyList<EncuestaListItemViewModel>>
{
    public async Task<IReadOnlyList<EncuestaListItemViewModel>> Handle(GetEncuestasQuery request, CancellationToken cancellationToken)
        => await db.Encuestas
            .OrderByDescending(e => e.Version)
            .Select(e => new EncuestaListItemViewModel(
                e.Id,
                e.Nombre,
                e.Version,
                e.Estado,
                e.PublicadaUtc,
                e.RetiradaUtc,
                e.Preguntas.Count(),
                db.EvaluacionesCalidad.Count(c => c.EncuestaId == e.Id)))
            .ToListAsync(cancellationToken);
}

public record GetEncuestaQuery(int EncuestaId) : IRequest<EncuestaViewModel>;

public class GetEncuestaQueryHandler(IEncuestaRepository encuestas) : IRequestHandler<GetEncuestaQuery, EncuestaViewModel>
{
    public async Task<EncuestaViewModel> Handle(GetEncuestaQuery request, CancellationToken cancellationToken)
    {
        var encuesta = await encuestas.GetByIdAsync(request.EncuestaId, cancellationToken)
            ?? throw new NotFoundException("La encuesta", request.EncuestaId);
        return EncuestaViewModel.Desde(encuesta);
    }
}

// ---------------------------------------------------------------- Nueva versión

/// <summary>Crea el borrador de la siguiente versión, copiando las preguntas de la vigente. Devuelve su Id.</summary>
public record CrearVersionEncuestaCommand : IRequest<int>;

public class CrearVersionEncuestaCommandHandler(IEncuestaRepository encuestas, IUnitOfWork uow)
    : IRequestHandler<CrearVersionEncuestaCommand, int>
{
    public const string NombrePorDefecto = "Encuesta de satisfacción del servicio";

    public async Task<int> Handle(CrearVersionEncuestaCommand request, CancellationToken cancellationToken)
    {
        if (await encuestas.ExisteBorradorAsync(cancellationToken))
            throw new ConflictException("Ya hay una versión en borrador: edítela, publíquela o descártela.");

        var version = await encuestas.GetUltimaVersionAsync(cancellationToken) + 1;
        var vigente = await encuestas.GetVigenteAsync(cancellationToken);
        var borrador = vigente is not null
            ? Encuesta.NuevaVersionDe(vigente, version)
            : Encuesta.CrearBorrador(NombrePorDefecto, version, []);

        encuestas.Add(borrador);
        await uow.SaveChangesAsync(cancellationToken);
        return borrador.Id;
    }
}

// ---------------------------------------------------------------- Edición del borrador

public record OpcionInput(int Valor, string Etiqueta);

public record PreguntaInput(
    string Texto,
    TipoPreguntaEnum Tipo,
    bool Obligatoria,
    bool PermiteComentario,
    IReadOnlyList<OpcionInput>? Opciones);

/// <summary>Reemplaza nombre y preguntas del borrador. El orden de las preguntas es el de la lista.</summary>
public record ActualizarBorradorEncuestaCommand(int EncuestaId, string Nombre, IReadOnlyList<PreguntaInput> Preguntas) : IRequest;

public class ActualizarBorradorEncuestaCommandValidator : AbstractValidator<ActualizarBorradorEncuestaCommand>
{
    public ActualizarBorradorEncuestaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("Indique el nombre de la encuesta.")
            .MaximumLength(Encuesta.NombreMaxLength);
        RuleFor(x => x.Preguntas).NotNull().WithMessage("Indique las preguntas.");
        RuleFor(x => x.Preguntas.Count).LessThanOrEqualTo(Encuesta.MaxPreguntas).When(x => x.Preguntas is not null)
            .WithMessage($"La encuesta no puede tener más de {Encuesta.MaxPreguntas} preguntas.");
        RuleForEach(x => x.Preguntas).ChildRules(p =>
        {
            p.RuleFor(x => x.Tipo).IsInEnum().WithMessage("El tipo de pregunta no es válido.");
            p.RuleFor(x => x.Texto).NotEmpty().WithMessage("Toda pregunta debe tener texto.")
                .MaximumLength(PreguntaEncuesta.TextoMaxLength);
        });
    }
}

public class ActualizarBorradorEncuestaCommandHandler(IEncuestaRepository encuestas, IUnitOfWork uow)
    : IRequestHandler<ActualizarBorradorEncuestaCommand, Unit>
{
    public async Task<Unit> Handle(ActualizarBorradorEncuestaCommand request, CancellationToken cancellationToken)
    {
        var encuesta = await encuestas.GetByIdAsync(request.EncuestaId, cancellationToken)
            ?? throw new NotFoundException("La encuesta", request.EncuestaId);

        encuesta.ActualizarBorrador(request.Nombre, request.Preguntas
            .Select(p => new DefinicionPregunta(
                p.Texto,
                p.Tipo,
                p.Obligatoria,
                p.PermiteComentario,
                PreguntaEncuesta.UsaOpciones(p.Tipo)
                    ? (p.Opciones ?? []).Select(o => new DefinicionOpcion(o.Valor, o.Etiqueta)).ToList()
                    : null))
            .ToList());

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------- Publicar / descartar

/// <summary>Publica el borrador: pasa a ser la vigente y la anterior queda retirada.</summary>
public record PublicarEncuestaCommand(int EncuestaId) : IRequest;

public class PublicarEncuestaCommandHandler(IEncuestaRepository encuestas, IUnitOfWork uow, TimeProvider timeProvider)
    : IRequestHandler<PublicarEncuestaCommand, Unit>
{
    public async Task<Unit> Handle(PublicarEncuestaCommand request, CancellationToken cancellationToken)
    {
        var encuesta = await encuestas.GetByIdAsync(request.EncuestaId, cancellationToken)
            ?? throw new NotFoundException("La encuesta", request.EncuestaId);
        var vigente = await encuestas.GetVigenteAsync(cancellationToken);

        encuesta.Publicar(vigente, timeProvider.GetUtcNow().UtcDateTime);
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record DescartarBorradorEncuestaCommand(int EncuestaId) : IRequest;

public class DescartarBorradorEncuestaCommandHandler(IEncuestaRepository encuestas, IUnitOfWork uow)
    : IRequestHandler<DescartarBorradorEncuestaCommand, Unit>
{
    public async Task<Unit> Handle(DescartarBorradorEncuestaCommand request, CancellationToken cancellationToken)
    {
        var encuesta = await encuestas.GetByIdAsync(request.EncuestaId, cancellationToken)
            ?? throw new NotFoundException("La encuesta", request.EncuestaId);
        if (encuesta.Estado != EstadoEncuestaEnum.Borrador)
            throw new ConflictException("Solo se puede descartar una versión en borrador.");

        encuestas.Remove(encuesta);
        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
