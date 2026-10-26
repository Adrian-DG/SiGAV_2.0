using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// App: la unidad principal envía un evento que le asignó el call center, completado en campo. El
/// cuerpo es el mismo de <see cref="RegistrarEventoCommand"/> (y se valida igual): reemplaza ubicación,
/// dirección, comentario, tipos, vehículos y personas, aplica la llegada si aún estaba pendiente, el
/// agente que lo atiende y, con fecha de cierre, lo completa. La unidad, el canal, la fecha de reporte
/// y el tramo (si no se indica) se conservan.
/// Idempotente por <see cref="RegistrarEventoCommand.RequestId"/>: si el evento ya se completó con esa
/// misma clave (la respuesta se perdió y la cola offline reintenta), devuelve lo ya guardado con
/// EsDuplicado = true. Un evento completado no se modifica.
/// </summary>
public record ActualizarEventoCommand(int EventoId, RegistrarEventoCommand Datos) : IRequest<RegistrarEventoResult>;

// Validator
public class ActualizarEventoCommandValidator : AbstractValidator<ActualizarEventoCommand>
{
    public ActualizarEventoCommandValidator(IValidator<RegistrarEventoCommand> datos)
    {
        RuleFor(x => x.Datos).NotNull().SetValidator(datos);
    }
}

// Handler
public class ActualizarEventoCommandHandler(
    IReadDbContext db,
    IEventoRepository eventos,
    IAgenteRepository agentes,
    IHistoricoRepository historico,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<ActualizarEventoCommand, RegistrarEventoResult>
{
    public async Task<RegistrarEventoResult> Handle(ActualizarEventoCommand request, CancellationToken cancellationToken)
    {
        if (EventoAcceso.EsWeb(currentUser))
            throw new ForbiddenException("Solo la unidad que atiende el evento puede completarlo desde la app.");

        var datos = request.Datos;
        var evento = await eventos.GetParaActualizarAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException("El evento", request.EventoId);
        // 404 si la unidad no participa; 403 si participa como apoyo
        EventoAcceso.AsegurarPuedeOperar(evento, currentUser);

        if (evento.Estado == EstadoEventoEnum.Completado)
        {
            if (datos.RequestId is { } rid && evento.RequestId == rid)
                return await InvolucradosEvento.ResultadoExistenteAsync(db, evento.Id, datos.Vehiculos, datos.Ciudadanos, cancellationToken);
            throw new ConflictException("El evento ya fue completado y no puede modificarse.");
        }

        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var unidadId = evento.UnidadPrincipal.UnidadId;
        var agenteId = EventoAcceso.AgenteDeLaSesion(currentUser, datos.AgenteId);
        await EventoAcceso.AsegurarAgenteVigenteAsync(agentes, currentUser, agenteId, cancellationToken);

        evento.ReemplazarDatos(
            datos.RequestId!.Value,
            new Coordenada(datos.Latitud, datos.Longitud),
            datos.MunicipioId,
            datos.TramoId ?? evento.TramoId,
            datos.Direccion,
            datos.Comentario,
            datos.TipoEventoIds);

        var (vehiculos, ciudadanos) =
            await InvolucradosEvento.AgregarAsync(evento, datos.Vehiculos, datos.Ciudadanos, historico, cancellationToken);

        if (evento.UnidadPrincipal.AgenteId != agenteId)
            evento.CambiarAgenteUnidad(unidadId, agenteId);
        // La llegada que registró el call center o "Llegué" se respeta; si seguía pendiente, la del agente
        if (evento.Estado == EstadoEventoEnum.Pendiente && datos.FechaHoraLlegadaUtc is { } llegada)
            evento.IniciarAtencion(llegada < evento.FechaHoraReporteUtc ? evento.FechaHoraReporteUtc : llegada, ahoraUtc);
        if (datos.FechaHoraCompletadoUtc is { } completado)
            evento.Completar(completado, datos.TipoCierreId!.Value, ahoraUtc);

        await uow.SaveChangesAsync(cancellationToken);

        return new RegistrarEventoResult(
            evento.Id,
            EsDuplicado: false,
            vehiculos.Select(v => v.Id).ToList(),
            ciudadanos.Select(c => c.Id).ToList());
    }
}
