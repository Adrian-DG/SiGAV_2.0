using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Agentes;

// Command
public record UpdateAgenteCommand(
    int AgenteId,
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    int RangoId,
    AreaOperativaEnum AreaOperativa,
    bool AccesoTotal = false,
    string? Especialidad = null) : IRequest, IDatosAgente;

// Validator
public class UpdateAgenteCommandValidator : DatosAgenteValidator<UpdateAgenteCommand>
{
    public UpdateAgenteCommandValidator(IReadDbContext db) : base(db)
    {
        RuleFor(x => x.AgenteId).GreaterThan(0).WithMessage("El agente es requerido.");
    }
}

// Handler
public class UpdateAgenteCommandHandler(IAgenteRepository agentes, IUnitOfWork uow) : IRequestHandler<UpdateAgenteCommand, Unit>
{
    public async Task<Unit> Handle(UpdateAgenteCommand request, CancellationToken cancellationToken)
    {
        var agente = await agentes.GetByIdAsync(request.AgenteId, cancellationToken)
            ?? throw new NotFoundException("El agente", request.AgenteId);

        var identificacion = Agente.NormalizarIdentificacion(request.Identificacion);
        if (await agentes.ExisteIdentificacionAsync(identificacion, excluirAgenteId: agente.Id, cancellationToken))
            throw new ConflictException($"La cédula '{identificacion}' ya está registrada en otro agente.");

        agente.ActualizarDatos(
            identificacion, request.Nombre, request.Apellido, request.Sexo, request.Institucion,
            request.RangoId, request.AreaOperativa, request.AccesoTotal, request.Especialidad);

        await uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
