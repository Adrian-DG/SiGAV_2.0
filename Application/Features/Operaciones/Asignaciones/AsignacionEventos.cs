using Application.Common;
using Application.Contracts;
using Application.Contracts.Notificaciones;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Asignaciones;

// Query: unidades con sesión abierta en la app y si pueden recibir un evento (disponibles primero).
public record GetUnidadesDisponiblesQuery : IRequest<IReadOnlyList<UnidadDisponibilidadViewModel>>;

public class GetUnidadesDisponiblesQueryHandler(IReadDbContext db, IConexionesUnidades conexiones, TimeProvider timeProvider)
    : IRequestHandler<GetUnidadesDisponiblesQuery, IReadOnlyList<UnidadDisponibilidadViewModel>>
{
    public async Task<IReadOnlyList<UnidadDisponibilidadViewModel>> Handle(GetUnidadesDisponiblesQuery request, CancellationToken cancellationToken)
        => await DisponibilidadUnidades.CalcularAsync(db, conexiones, timeProvider.GetUtcNow().UtcDateTime, null, cancellationToken);
}

/// <summary>
/// Evento creado por el call center y asignado a una unidad disponible. Queda pendiente: la unidad lo
/// ve en la app (GET /eventos/asignados) y recibe una notificación si está conectada.
/// <see cref="AgenteId"/>: el agente de la unidad que lo atiende; sin indicar, el primero libre
/// (el de la sesión antes que su componente).
/// </summary>
public record AsignarEventoCommand(
    decimal Latitud,
    decimal Longitud,
    int MunicipioId,
    IReadOnlyList<int> TipoEventoIds,
    int UnidadId,
    int? AgenteId = null,
    int? TramoId = null,
    string? Direccion = null,
    string? Comentario = null,
    CanalReporteEnum CanalReporte = CanalReporteEnum.CallCenter) : IRequest<AsignarEventoResult>;

/// <summary><see cref="Notificada"/>: la unidad tenía la app conectada y se le envió la notificación.</summary>
public record AsignarEventoResult(int Id, int UnidadId, int AgenteId, bool Notificada);

public class AsignarEventoCommandValidator : AbstractValidator<AsignarEventoCommand>
{
    private const int MaxTipos = 20;

    public AsignarEventoCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.Latitud).InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90.");
        RuleFor(x => x.Longitud).InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180.");
        RuleFor(x => x.MunicipioId)
            .GreaterThan(0).WithMessage("El municipio es requerido.")
            .MustAsync((id, ct) => db.Municipios.ExisteActivoAsync(id, ct)).WithMessage("El municipio especificado no existe.");
        RuleFor(x => x.TramoId!.Value)
            .MustAsync((id, ct) => db.Tramos.ExisteActivoAsync(id, ct)).WithMessage("El tramo especificado no existe.")
            .When(x => x.TramoId is > 0);
        RuleFor(x => x.TipoEventoIds)
            .NotEmpty().WithMessage("Seleccione al menos un tipo de evento.")
            .Must(t => t is null || t.Count <= MaxTipos).WithMessage($"No se pueden indicar más de {MaxTipos} tipos de evento.")
            .MustAsync((ids, ct) => db.TiposEvento.ExistenActivosAsync(ids, ct))
            .WithMessage("Uno o más tipos de evento no existen.");
        RuleFor(x => x.UnidadId).GreaterThan(0).WithMessage("Seleccione la unidad que atenderá el evento.");
        RuleFor(x => x.AgenteId).GreaterThan(0).When(x => x.AgenteId.HasValue).WithMessage("El agente no es válido.");
        RuleFor(x => x.Direccion).MaximumLength(Evento.DireccionMaxLength);
        RuleFor(x => x.Comentario).MaximumLength(Evento.ComentarioMaxLength);
        RuleFor(x => x.CanalReporte)
            .IsInEnum().WithMessage("El canal de reporte no es válido.")
            .NotEqual(CanalReporteEnum.AgenteCampo).WithMessage("Un evento asignado no puede tener el canal Agente en campo.");
    }
}

public class AsignarEventoCommandHandler(
    IEventoRepository eventos,
    IUnidadRepository unidades,
    IReadDbContext db,
    IConexionesUnidades conexiones,
    INotificadorUnidades notificador,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<AsignarEventoCommand, AsignarEventoResult>
{
    public async Task<AsignarEventoResult> Handle(AsignarEventoCommand request, CancellationToken cancellationToken)
    {
        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;

        var disponibilidad = (await DisponibilidadUnidades.CalcularAsync(db, conexiones, ahoraUtc, request.UnidadId, cancellationToken))
            .FirstOrDefault()
            ?? throw new ConflictException("La unidad no tiene una sesión abierta en la app: no puede recibir eventos.");

        AgenteDisponibilidadViewModel agente;
        if (request.AgenteId is { } agenteId)
        {
            agente = disponibilidad.Agentes.FirstOrDefault(a => a.AgenteId == agenteId)
                ?? throw new ConflictException($"El agente indicado no opera la unidad {disponibilidad.Ficha} en esta sesión.");
            if (!agente.Disponible)
                throw new ConflictException((agente.EventoId is { } eventoId
                    ? $"{agente.Rango} {agente.Nombre} ya atiende el evento No. {eventoId}."
                    : $"{agente.Rango} {agente.Nombre} ya atiende un evento registrado en la app.").Trim());
        }
        else
        {
            agente = disponibilidad.Agentes.FirstOrDefault(a => a.Disponible)
                ?? throw new ConflictException($"La unidad {disponibilidad.Ficha} no tiene agentes disponibles: todos atienden un evento.");
        }

        var unidad = await unidades.GetConDenominacionAsync(request.UnidadId, cancellationToken)
            ?? throw new NotFoundException("La unidad", request.UnidadId);

        var evento = Evento.Registrar(
            requestId: null,
            request.CanalReporte,
            new Coordenada(request.Latitud, request.Longitud),
            request.MunicipioId,
            request.TramoId ?? unidad.Denominacion?.TramoId,
            request.Direccion,
            request.Comentario,
            ahoraUtc,
            request.TipoEventoIds,
            unidad,
            agente.AgenteId,
            ahoraUtc);

        eventos.Add(evento);
        await uow.SaveChangesAsync(cancellationToken);

        var tipos = await db.TiposEvento
            .Where(t => request.TipoEventoIds.Contains(t.Id))
            .OrderBy(t => t.Nombre)
            .Select(t => t.Nombre)
            .ToListAsync(cancellationToken);
        var mensaje = string.Join(", ", tipos) + (evento.Direccion is { } direccion ? $" · {direccion}" : string.Empty);

        var notificada = await NotificarAsync(
            [unidad.Id],
            new NotificacionUnidad(TiposNotificacion.EventoAsignado, $"Evento asignado No. {evento.Id}", mensaje, ahoraUtc, evento.Id, agente.AgenteId),
            cancellationToken);

        return new AsignarEventoResult(evento.Id, unidad.Id, agente.AgenteId, notificada);
    }

    /// <summary>
    /// El evento ya quedó guardado: un fallo al notificar no lo deshace (la app igual lo ve al
    /// consultar sus eventos asignados).
    /// </summary>
    private async Task<bool> NotificarAsync(IReadOnlyCollection<int> unidadIds, NotificacionUnidad notificacion, CancellationToken cancellationToken)
    {
        if (!unidadIds.Any(conexiones.EstaConectada)) return false;
        try
        {
            await notificador.NotificarUnidadesAsync(unidadIds, notificacion, cancellationToken);
            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}

/// <summary>Mensaje del call center a las unidades indicadas o, sin indicar, a todas las conectadas.</summary>
public record EnviarNotificacionCommand(string Titulo, string Mensaje, IReadOnlyList<int>? UnidadIds = null) : IRequest<EnviarNotificacionResult>;

/// <summary><see cref="UnidadesConectadas"/>: cuántas de las destinatarias la recibieron (tenían la app conectada).</summary>
public record EnviarNotificacionResult(int UnidadesConectadas);

public class EnviarNotificacionCommandValidator : AbstractValidator<EnviarNotificacionCommand>
{
    public const int TituloMaxLength = 100;
    public const int MensajeMaxLength = 500;
    public const int MaxUnidades = 500;

    public EnviarNotificacionCommandValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es requerido.")
            .MaximumLength(TituloMaxLength).WithMessage($"El título no puede exceder {TituloMaxLength} caracteres.");
        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("El mensaje es requerido.")
            .MaximumLength(MensajeMaxLength).WithMessage($"El mensaje no puede exceder {MensajeMaxLength} caracteres.");
        RuleFor(x => x.UnidadIds)
            .Must(ids => ids is null || ids.Count > 0).WithMessage("Seleccione al menos una unidad o envíe a todas.")
            .Must(ids => ids is null || ids.Count <= MaxUnidades).WithMessage($"No se puede enviar a más de {MaxUnidades} unidades a la vez.");
    }
}

public class EnviarNotificacionCommandHandler(
    INotificadorUnidades notificador,
    IConexionesUnidades conexiones,
    TimeProvider timeProvider) : IRequestHandler<EnviarNotificacionCommand, EnviarNotificacionResult>
{
    public async Task<EnviarNotificacionResult> Handle(EnviarNotificacionCommand request, CancellationToken cancellationToken)
    {
        var notificacion = new NotificacionUnidad(
            TiposNotificacion.General,
            request.Titulo.Trim(),
            request.Mensaje.Trim(),
            timeProvider.GetUtcNow().UtcDateTime);

        if (request.UnidadIds is null)
        {
            await notificador.NotificarTodasAsync(notificacion, cancellationToken);
            return new EnviarNotificacionResult(conexiones.Conectadas().Count);
        }

        var unidadIds = request.UnidadIds.Distinct().ToList();
        await notificador.NotificarUnidadesAsync(unidadIds, notificacion, cancellationToken);
        return new EnviarNotificacionResult(unidadIds.Count(conexiones.EstaConectada));
    }
}
