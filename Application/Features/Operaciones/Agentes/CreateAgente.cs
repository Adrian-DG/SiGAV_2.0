using Application.Contracts.Operaciones;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Operaciones.Agentes;

// Command: alta desde front desk; el agente queda autorizado de inmediato
public record CreateAgenteCommand(
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    int RangoId,
    AreaOperativaEnum AreaOperativa,
    bool AccesoTotal = false,
    string? Especialidad = null) : IRequest<int>, IDatosAgente;

// Validator
public class CreateAgenteCommandValidator(ICatalogoQueries catalogos) : DatosAgenteValidator<CreateAgenteCommand>(catalogos);

// Handler
public class CreateAgenteCommandHandler(IAgenteRepository agentes, IUnitOfWork uow) : IRequestHandler<CreateAgenteCommand, int>
{
    public async Task<int> Handle(CreateAgenteCommand request, CancellationToken cancellationToken)
    {
        var agente = Agente.Crear(
            request.Identificacion, request.Nombre, request.Apellido, request.Sexo, request.Institucion,
            request.RangoId, request.AreaOperativa, request.AccesoTotal, request.Especialidad,
            autorizado: true);

        if (await agentes.ExisteIdentificacionAsync(agente.Identificacion, cancellationToken: cancellationToken))
            throw new ConflictException($"La cédula '{agente.Identificacion}' ya está registrada.");

        agentes.Add(agente);
        await uow.SaveChangesAsync(cancellationToken);

        return agente.Id;
    }
}
