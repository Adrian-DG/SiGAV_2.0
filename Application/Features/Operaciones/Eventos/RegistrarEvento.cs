using Application.Contracts;
using Application.Common;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Eventos;

public record VehiculoEventoRequest(
    string? Placa,
    int? TipoVehiculoId,
    int? MarcaId,
    int? ModeloId,
    int? ColorId);

public record CiudadanoEventoRequest(
    RolCiudadanoEnum Rol,
    string? Identificacion,
    string? Nombre,
    string? Apellido,
    SexoEnum Sexo = SexoEnum.NONE,
    string? Telefono = null,
    int? NacionalidadId = null,
    VehiculoEventoRequest? Vehiculo = null);

/// <summary>
/// Registra un evento. Es idempotente por <see cref="RequestId"/>: un reenvío (cola offline de la app)
/// devuelve el evento ya registrado con EsDuplicado = true, sin crear otro.
/// Desde la app, la unidad y el agente salen de la sesión (UnidadId/AgenteId del cuerpo se ignoran),
/// el tramo por defecto es el de la denominación de la unidad
/// y el canal es siempre AgenteCampo. Puede enviarse ya atendido o completado (llegada y cierre).
/// </summary>
public record RegistrarEventoCommand(
    Guid? RequestId,
    decimal Latitud,
    decimal Longitud,
    IReadOnlyList<int> TipoEventoIds,
    DateTime? FechaHoraReporteUtc = null,
    int? TramoId = null,
    string? Direccion = null,
    string? Comentario = null,
    CanalReporteEnum? CanalReporte = null,
    int? UnidadId = null,
    int? AgenteId = null,
    DateTime? FechaHoraLlegadaUtc = null,
    DateTime? FechaHoraCompletadoUtc = null,
    TipoCierreEventoEnum? TipoCierre = null,
    IReadOnlyList<CiudadanoEventoRequest>? Ciudadanos = null) : IRequest<RegistrarEventoResult>;

// Validator
public class RegistrarEventoCommandValidator : AbstractValidator<RegistrarEventoCommand>
{
    private const int MaxTipos = 20;
    private const int MaxCiudadanos = 30;

    public RegistrarEventoCommandValidator(IReadDbContext db, ICurrentUserService currentUser)
    {
        var esWeb = EventoAcceso.EsWeb(currentUser);

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

        RuleFor(x => x.Direccion).MaximumLength(Evento.DireccionMaxLength);
        RuleFor(x => x.Comentario).MaximumLength(Evento.ComentarioMaxLength);
        RuleFor(x => x.TipoCierre)
            .NotNull().When(x => x.FechaHoraCompletadoUtc.HasValue)
            .WithMessage("Indique el tipo de cierre del evento completado.");
        RuleFor(x => x.Ciudadanos)
            .Must(c => c is null || c.Count <= MaxCiudadanos)
            .WithMessage($"No se pueden registrar más de {MaxCiudadanos} personas en un evento.");

        if (esWeb)
        {
            // WithMessage aplica solo a la regla anterior: cada una lleva el suyo
            const string unidadRequerida = "Indique la unidad principal del evento.";
            const string agenteRequerido = "Indique el agente de la unidad principal.";
            RuleFor(x => x.UnidadId).NotNull().WithMessage(unidadRequerida).GreaterThan(0).WithMessage(unidadRequerida);
            RuleFor(x => x.AgenteId).NotNull().WithMessage(agenteRequerido).GreaterThan(0).WithMessage(agenteRequerido);
        }
        else
        {
            // La cola offline reintenta: sin clave no se pueden detectar los reenvíos.
            const string requestIdRequerido = "La clave de idempotencia (requestId) es requerida.";
            RuleFor(x => x.RequestId)
                .NotNull().WithMessage(requestIdRequerido)
                .NotEqual(Guid.Empty).WithMessage(requestIdRequerido);
            // Se sincroniza tarde: la fecha debe ser la del reporte, no la de la sincronización.
            RuleFor(x => x.FechaHoraReporteUtc).NotNull().WithMessage("La fecha y hora del reporte es requerida.");
        }
    }
}

// Handler
public class RegistrarEventoCommandHandler(
    IEventoRepository eventos,
    IUnidadRepository unidades,
    IAgenteRepository agentes,
    IHistoricoRepository historico,
    ICurrentUserService currentUser,
    IUnitOfWork uow,
    TimeProvider timeProvider) : IRequestHandler<RegistrarEventoCommand, RegistrarEventoResult>
{
    public async Task<RegistrarEventoResult> Handle(RegistrarEventoCommand request, CancellationToken cancellationToken)
    {
        // Reenvío de algo ya registrado: se devuelve el mismo evento
        if (request.RequestId is { } requestId
            && await eventos.GetIdByRequestIdAsync(requestId, cancellationToken) is { } existente)
            return new RegistrarEventoResult(existente, EsDuplicado: true);

        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var esWeb = EventoAcceso.EsWeb(currentUser);

        var (unidadId, agenteId, canal) = esWeb
            ? (request.UnidadId!.Value, request.AgenteId!.Value, request.CanalReporte ?? CanalReporteEnum.CallCenter)
            : (currentUser.UnidadId ?? throw new ForbiddenException("La sesión no tiene una unidad asociada."),
               currentUser.AgenteId ?? throw new ForbiddenException("La sesión no tiene un agente asociado."),
               CanalReporteEnum.AgenteCampo);

        var unidad = await unidades.GetConDenominacionAsync(unidadId, cancellationToken)
            ?? throw new NotFoundException("La unidad", unidadId);
        if (esWeb && await agentes.GetByIdAsync(agenteId, cancellationToken) is not { IsActive: true })
            throw new NotFoundException("El agente", agenteId);

        // Evento de campo: si la app no indica el tramo, es el de la denominación actual de la unidad
        var tramoId = request.TramoId ?? (esWeb ? null : unidad.Denominacion?.TramoId);

        var evento = Evento.Registrar(
            request.RequestId,
            canal,
            new Coordenada(request.Latitud, request.Longitud),
            request.MunicipioId,
            tramoId,
            request.Direccion,
            request.Comentario,
            request.FechaHoraReporteUtc ?? ahoraUtc,
            request.TipoEventoIds,
            unidad,
            agenteId,
            ahoraUtc);

        foreach (var c in request.Ciudadanos ?? [])
        {
            var persona = new DatosPersona(c.Identificacion, c.Nombre, c.Apellido, c.Sexo, c.Telefono, c.NacionalidadId);
            var vehiculo = c.Vehiculo is { } v
                ? new DatosVehiculo(v.Placa, v.TipoVehiculoId, v.MarcaId, v.ModeloId, v.ColorId, v.MarcaTexto, v.ModeloTexto, v.ColorTexto)
                : null;

            // Vínculo con los maestros históricos cuando la persona/el vehículo ya existen en ellos
            var ciudadano = persona.Identificacion is { } identificacion
                ? await historico.GetCiudadanoAsync(identificacion, cancellationToken)
                : null;
            var vehiculoHistorico = vehiculo?.Placa is { } placa
                ? await historico.GetVehiculoAsync(placa, cancellationToken)
                : null;

            evento.AgregarCiudadano(c.Rol, persona, vehiculo, ciudadano, vehiculoHistorico);
        }

        // Reportado ya atendido (p. ej. desde la cola offline): se aplican llegada y cierre
        if (request.FechaHoraLlegadaUtc is { } llegada)
            evento.IniciarAtencion(llegada, ahoraUtc);
        if (request.FechaHoraCompletadoUtc is { } completado)
            evento.Completar(completado, request.TipoCierre!.Value, ahoraUtc);

        eventos.Add(evento);

        try
        {
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException) when (request.RequestId is { } rid)
        {
            // Dos envíos simultáneos con la misma clave: el otro ganó la carrera
            var ganador = await eventos.GetIdByRequestIdAsync(rid, cancellationToken);
            if (ganador is null) throw;
            return new RegistrarEventoResult(ganador.Value, EsDuplicado: true);
        }

        return new RegistrarEventoResult(evento.Id, EsDuplicado: false);
    }
}
