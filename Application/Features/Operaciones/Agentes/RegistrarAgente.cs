using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Operaciones.Agentes;

// Command: auto-registro desde la app (anónimo). El agente queda PENDIENTE de autorización
// por front desk y no puede otorgarse acceso total. (En SiGAV 1.0 quedaba autorizado de una vez.)
public record RegistrarAgenteCommand(
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    int RangoId,
    AreaOperativaEnum AreaOperativa,
    string? Especialidad = null) : IRequest, IDatosAgente;

// Validator
public class RegistrarAgenteCommandValidator(IReadDbContext db) : DatosAgenteValidator<RegistrarAgenteCommand>(db);

// Handler
public class RegistrarAgenteCommandHandler(IAgenteRepository agentes, IUnitOfWork uow) : IRequestHandler<RegistrarAgenteCommand, Unit>
{
    public async Task<Unit> Handle(RegistrarAgenteCommand request, CancellationToken cancellationToken)
    {
        var agente = Agente.Crear(
            request.Identificacion, request.Nombre, request.Apellido, request.Sexo, request.Institucion,
            request.RangoId, request.AreaOperativa, accesoTotal: false, request.Especialidad,
            autorizado: false);

        if (await agentes.ExisteIdentificacionAsync(agente.Identificacion, cancellationToken: cancellationToken))
            throw new ConflictException("Ya existe un agente registrado con esta cédula.");

        agentes.Add(agente);

        await uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
