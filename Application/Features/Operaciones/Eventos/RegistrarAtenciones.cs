using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Persona atendida por la ambulancia y su atención. Si ya está en el evento (por la clave de la
/// atención, o por identificación o nombre) se le registra la atención; si no, se agrega con rol Paciente.
/// </summary>
public record AtencionPacienteRequest(
    AtencionPrehospitalariaRequest Atencion,
    string? Identificacion = null,
    string? Nombre = null,
    string? Apellido = null,
    SexoEnum Sexo = SexoEnum.NONE,
    string? Telefono = null,
    int? NacionalidadId = null,
    int? Edad = null);

/// <summary>
/// App: la ambulancia registra la atención prehospitalaria de sus pacientes en un evento en que
/// participa con cualquier rol (en particular como apoyo, que no puede completar el evento con PUT).
/// Solo toca a esas personas: no reemplaza ubicación, tipos, vehículos ni al resto de personas. Se
/// admite con el evento ya completado. Si la unidad era un apoyo solicitado, registrar la atención
/// confirma su llegada. Idempotente por <see cref="AtencionPrehospitalariaRequest.RequestId"/>.
/// </summary>
public record RegistrarAtencionesCommand(int EventoId, IReadOnlyList<AtencionPacienteRequest> Pacientes)
    : IRequest<RegistrarAtencionesResult>;

/// <summary>CiudadanoIds: Id en el evento de cada paciente, en el orden del request.</summary>
public record RegistrarAtencionesResult(int EventoId, IReadOnlyList<int> CiudadanoIds);

public class RegistrarAtencionesCommandValidator : AbstractValidator<RegistrarAtencionesCommand>
{
    public RegistrarAtencionesCommandValidator(IReadDbContext db, TimeProvider timeProvider)
    {
        RuleFor(x => x.Pacientes)
            .NotEmpty().WithMessage("Indique al menos un paciente atendido.")
            .Must(p => p is null || p.Count <= Evento.MaxCiudadanos)
            .WithMessage($"No se pueden registrar más de {Evento.MaxCiudadanos} personas en un evento.")
            .Must(p => p is null || p.Select(x => x.Atencion?.RequestId).Distinct().Count() == p.Count)
            .WithMessage("Hay atenciones con la misma clave (requestId).");

        RuleForEach(x => x.Pacientes).ChildRules(p =>
        {
            p.RuleFor(x => x.Atencion).NotNull().WithMessage("Falta la atención del paciente.");
            // La cola offline reintenta: sin clave, un paciente desconocido se duplicaría en cada reenvío
            p.RuleFor(x => x.Atencion.RequestId)
                .NotNull().WithMessage("La clave de la atención (requestId) es requerida.")
                .When(x => x.Atencion is not null);
            p.RuleFor(x => x.Atencion)
                .SetValidator(new AtencionPrehospitalariaRequestValidator(db, timeProvider))
                .When(x => x.Atencion is not null);
            p.RuleFor(x => x.Edad)
                .InclusiveBetween(0, DatosPersona.EdadMaxima)
                .WithMessage($"La edad debe estar entre 0 y {DatosPersona.EdadMaxima} años.");
        });
    }
}

public class RegistrarAtencionesCommandHandler(
    IEventoRepository eventos,
    IHistoricoRepository historico,
    ICurrentUserService currentUser,
    IUnitOfWork uow) : IRequestHandler<RegistrarAtencionesCommand, RegistrarAtencionesResult>
{
    public async Task<RegistrarAtencionesResult> Handle(RegistrarAtencionesCommand request, CancellationToken cancellationToken)
    {
        if (EventoAcceso.EsWeb(currentUser))
            throw new ForbiddenException("Solo la ambulancia que atendió al paciente registra la atención desde la app.");

        var evento = await eventos.GetParaEvidenciasAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        // 404 si la unidad no participa
        EventoAcceso.AsegurarParticipa(evento, currentUser);
        if (!evento.IsActive) throw new ConflictException("El evento está anulado.");

        // Atender al paciente implica que el apoyo solicitado ya llegó
        var unidadId = EventoAcceso.UnidadDeAlcance(currentUser)!.Value;
        if (evento.Unidades.Any(u => u.UnidadId == unidadId && u.Rol == RolUnidadEventoEnum.ApoyoSolicitado))
            evento.ConfirmarLlegadaApoyo(unidadId);

        var personas = new List<EventoCiudadanoInfo>();
        foreach (var p in request.Pacientes)
        {
            var persona = new DatosPersona(p.Identificacion, p.Nombre, p.Apellido, p.Sexo, p.Telefono, p.NacionalidadId, p.Edad);
            var ciudadano = await InvolucradosEvento.CiudadanoHistoricoAsync(historico, persona, evento.FechaHoraReporteUtc, cancellationToken);
            personas.Add(evento.RegistrarAtencion(persona, p.Atencion.ToDominio(), ciudadano));
        }

        await uow.SaveChangesAsync(cancellationToken);
        return new RegistrarAtencionesResult(evento.Id, personas.Select(c => c.Id).ToList());
    }
}
