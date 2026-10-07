using Application.Contracts;
using Application.Common;
using Application.Exceptions;
using Application.Features.Catalogos;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Vehículo involucrado. <see cref="Clave"/> lo identifica dentro del request (la app lo genera sin
/// conexión, antes de que exista un Id) para que las personas indiquen en cuál iban.
/// <see cref="PlacaNoEstandar"/>: el agente confirmó que la placa no sigue los formatos del
/// catálogo (extranjera, temporal, ilegible...), así que no se valida su formato.
/// <see cref="TipoEventoIds"/>: tipos atendidos a este vehículo, de entre los del evento.
/// </summary>
public record VehiculoEventoRequest(
    string Clave,
    string? Placa,
    int? TipoVehiculoId,
    int? MarcaId,
    int? ModeloId,
    int? ColorId,
    string? MarcaTexto = null,
    string? ModeloTexto = null,
    string? ColorTexto = null,
    bool PlacaNoEstandar = false,
    IReadOnlyList<int>? TipoEventoIds = null);

/// <summary>
/// <see cref="VehiculoClave"/>: <see cref="VehiculoEventoRequest.Clave"/> del vehículo en que iba (null = sin vehículo).
/// <see cref="TipoEventoIds"/>: tipos atendidos a esta persona, de entre los del evento.
/// </summary>
public record CiudadanoEventoRequest(
    RolCiudadanoEnum Rol,
    string? Identificacion,
    string? Nombre,
    string? Apellido,
    SexoEnum Sexo = SexoEnum.NONE,
    string? Telefono = null,
    int? NacionalidadId = null,
    string? VehiculoClave = null,
    IReadOnlyList<int>? TipoEventoIds = null,
    int? Edad = null);

/// <summary>
/// Registra un evento. Es idempotente por <see cref="RequestId"/>: un reenvío (cola offline de la app)
/// devuelve el evento ya registrado con EsDuplicado = true, sin crear otro.
/// Desde la app, la unidad y el agente salen de la sesión (UnidadId/AgenteId del cuerpo se ignoran),
/// el tramo por defecto es el de la denominación de la unidad
/// y el canal es siempre AgenteCampo. Puede enviarse ya atendido o completado (llegada y cierre).
/// <see cref="TipoEventoIds"/> son todos los tipos del evento: incluyen los de cada vehículo y persona.
/// </summary>
public record RegistrarEventoCommand(
    Guid? RequestId,
    decimal Latitud,
    decimal Longitud,
    int MunicipioId,
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
    int? TipoCierreId = null,
    IReadOnlyList<VehiculoEventoRequest>? Vehiculos = null,
    IReadOnlyList<CiudadanoEventoRequest>? Ciudadanos = null) : IRequest<RegistrarEventoResult>;

// Validator
public class RegistrarEventoCommandValidator : AbstractValidator<RegistrarEventoCommand>
{
    private const int MaxTipos = 20;
    private const int ClaveMaxLength = 50;

    // Se cargan una vez por validación (el catálogo es pequeño)
    private IReadOnlyList<PrefijoPlacaItemViewModel>? _prefijos;

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
        RuleFor(x => x.TipoCierreId)
            .NotNull().When(x => x.FechaHoraCompletadoUtc.HasValue)
            .WithMessage("Indique el tipo de cierre del evento completado.");
        RuleFor(x => x.TipoCierreId!.Value)
            .MustAsync((id, ct) => db.TiposCierre.ExisteActivoAsync(id, ct)).WithMessage("El tipo de cierre no es válido.")
            .When(x => x.TipoCierreId.HasValue);
        RuleFor(x => x.Ciudadanos)
            .Must(c => c is null || c.Count <= Evento.MaxCiudadanos)
            .WithMessage($"No se pueden registrar más de {Evento.MaxCiudadanos} personas en un evento.");

        // ---- Vehículos
        RuleFor(x => x.Vehiculos)
            .Must(v => v is null || v.Count <= Evento.MaxVehiculos)
            .WithMessage($"No se pueden registrar más de {Evento.MaxVehiculos} vehículos en un evento.")
            .Must(v => v is null || v.Select(x => x.Clave).Distinct(StringComparer.Ordinal).Count() == v.Count)
            .WithMessage("Hay vehículos con la misma clave.")
            .Must(v => v is null || PlacasDistintas(v))
            .WithMessage("Hay vehículos con la misma placa.");
        RuleForEach(x => x.Vehiculos).ChildRules(v =>
        {
            v.RuleFor(x => x.Clave)
                .NotEmpty().WithMessage("Cada vehículo necesita una clave.")
                .MaximumLength(ClaveMaxLength);
        });
        RuleForEach(x => x.Vehiculos)
            .MustAsync(async (v, ct) => await PlacaConFormatoValidoAsync(db, v, ct))
            .WithMessage("La placa no tiene un formato válido. Si es extranjera, temporal o ilegible, márquela como no estándar.");
        RuleFor(x => x.Vehiculos)
            .MustAsync((v, ct) => db.TiposVehiculo.ExistenActivosAsync(v?.Select(x => x.TipoVehiculoId).OfType<int>(), ct))
            .WithMessage("Uno o más tipos de vehículo no existen.")
            .MustAsync((v, ct) => db.Marcas.ExistenActivosAsync(v?.Select(x => x.MarcaId).OfType<int>(), ct))
            .WithMessage("Una o más marcas no existen.")
            .MustAsync((v, ct) => db.Modelos.ExistenActivosAsync(v?.Select(x => x.ModeloId).OfType<int>(), ct))
            .WithMessage("Uno o más modelos no existen.")
            .MustAsync((v, ct) => db.Colores.ExistenActivosAsync(v?.Select(x => x.ColorId).OfType<int>(), ct))
            .WithMessage("Uno o más colores no existen.");

        // ---- Tipos de cada vehículo y persona: de entre los del evento
        RuleForEach(x => x.Vehiculos)
            .Must((cmd, v) => TiposDelEvento(cmd, v.TipoEventoIds))
            .WithMessage("Los tipos de un vehículo deben estar entre los tipos del evento.");
        RuleForEach(x => x.Ciudadanos)
            .Must((cmd, c) => TiposDelEvento(cmd, c.TipoEventoIds))
            .WithMessage("Los tipos de una persona deben estar entre los tipos del evento.");

        RuleForEach(x => x.Ciudadanos).ChildRules(c =>
        {
            c.RuleFor(x => x.Edad)
                .InclusiveBetween(0, DatosPersona.EdadMaxima)
                .WithMessage($"La edad debe estar entre 0 y {DatosPersona.EdadMaxima} años.");
        });

        // ---- Personas: en qué vehículo iban
        RuleForEach(x => x.Ciudadanos)
            .Must((cmd, c) => c.VehiculoClave is null || (cmd.Vehiculos ?? []).Any(v => v.Clave == c.VehiculoClave))
            .WithMessage("Una persona está asociada a un vehículo que no está en el evento.")
            .Must(c => c.VehiculoClave is not null || !EventoCiudadanoInfo.RequiereVehiculo(c.Rol))
            .WithMessage("Conductor y pasajero deben estar asociados a un vehículo del evento.")
            .Must(c => c.VehiculoClave is null || EventoCiudadanoInfo.AdmiteVehiculo(c.Rol))
            .WithMessage("Un peatón no puede estar asociado a un vehículo.");
        RuleFor(x => x.Ciudadanos)
            .Must(c => c is null || c
                .Where(x => x.Rol == RolCiudadanoEnum.Conductor && x.VehiculoClave is not null)
                .GroupBy(x => x.VehiculoClave)
                .All(g => g.Count() == 1))
            .WithMessage("Un vehículo no puede tener más de un conductor.");

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

    private static bool TiposDelEvento(RegistrarEventoCommand cmd, IReadOnlyList<int>? tipoEventoIds)
        => tipoEventoIds is null || tipoEventoIds.All(id => cmd.TipoEventoIds?.Contains(id) == true);

    private static bool PlacasDistintas(IReadOnlyList<VehiculoEventoRequest> vehiculos)
    {
        var placas = vehiculos.Select(v => NormalizarONull(v.Placa)).OfType<string>().ToList();
        return placas.Distinct(StringComparer.Ordinal).Count() == placas.Count;
    }

    /// <summary>
    /// Sin placa, o marcada como no estándar, no se valida. El tipo de vehículo NO se exige acorde
    /// al prefijo: la app solo lo advierte, porque hay placas mal asignadas que el agente confirma.
    /// </summary>
    private async Task<bool> PlacaConFormatoValidoAsync(IReadDbContext db, VehiculoEventoRequest vehiculo, CancellationToken cancellationToken)
    {
        if (vehiculo.PlacaNoEstandar || NormalizarONull(vehiculo.Placa) is not { } placa) return true;

        _prefijos ??= await ReglaPlaca.CargarAsync(db, cancellationToken);
        // Sin catálogo de prefijos (base sin sembrar) no hay contra qué validar
        return _prefijos.Count == 0 || ReglaPlaca.Identificar(placa, _prefijos) is not null;
    }

    /// <summary>La placa ya viene normalizada por la app; si no lo es, el dominio rechaza el dato.</summary>
    private static string? NormalizarONull(string? placa)
    {
        try { return DatosVehiculo.NormalizarPlaca(placa); }
        catch (Domain.Exceptions.DomainException) { return null; }
    }
}

// Handler
public class RegistrarEventoCommandHandler(
    IReadDbContext db,
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
            return await DuplicadoAsync(existente, cancellationToken);

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

        // Vehículos primero: las personas se asocian a ellos por su clave
        var vehiculoPorClave = new Dictionary<string, EventoVehiculoInfo>(StringComparer.Ordinal);
        var vehiculosCreados = new List<EventoVehiculoInfo>();
        var ciudadanosCreados = new List<EventoCiudadanoInfo>();
        foreach (var v in request.Vehiculos ?? [])
        {
            var datos = new DatosVehiculo(v.Placa, v.TipoVehiculoId, v.MarcaId, v.ModeloId, v.ColorId, v.MarcaTexto, v.ModeloTexto, v.ColorTexto);
            // Vínculo con el maestro histórico cuando la placa ya existe en él
            var vehiculoHistorico = datos.Placa is { } placa
                ? await historico.GetVehiculoAsync(placa, cancellationToken)
                : null;
            vehiculoPorClave[v.Clave] = evento.AgregarVehiculo(datos, vehiculoHistorico, v.TipoEventoIds);
            vehiculosCreados.Add(vehiculoPorClave[v.Clave]);
        }

        foreach (var c in request.Ciudadanos ?? [])
        {
            var persona = new DatosPersona(c.Identificacion, c.Nombre, c.Apellido, c.Sexo, c.Telefono, c.NacionalidadId, c.Edad);
            var ciudadano = persona.Identificacion is { } identificacion
                ? await historico.GetCiudadanoAsync(identificacion, cancellationToken)
                : null;
            var vehiculo = c.VehiculoClave is { } clave ? vehiculoPorClave[clave] : null;

            ciudadanosCreados.Add(evento.AgregarCiudadano(c.Rol, persona, vehiculo, ciudadano, c.TipoEventoIds));
        }

        // Reportado ya atendido (p. ej. desde la cola offline): se aplican llegada y cierre
        if (request.FechaHoraLlegadaUtc is { } llegada)
            evento.IniciarAtencion(llegada, ahoraUtc);
        if (request.FechaHoraCompletadoUtc is { } completado)
            evento.Completar(completado, request.TipoCierreId!.Value, ahoraUtc);

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
            return await DuplicadoAsync(ganador.Value, cancellationToken);
        }

        return new RegistrarEventoResult(
            evento.Id,
            EsDuplicado: false,
            vehiculosCreados.Select(v => v.Id).ToList(),
            ciudadanosCreados.Select(c => c.Id).ToList());
    }

    /// <summary>
    /// Reenvío: el mismo request ya se registró, así que vehículos y personas están en el orden en
    /// que se insertaron (el de aquel request), que es el orden de sus Ids.
    /// </summary>
    private async Task<RegistrarEventoResult> DuplicadoAsync(int eventoId, CancellationToken cancellationToken)
    {
        var vehiculoIds = await db.EventoVehiculos.Where(v => v.EventoId == eventoId).OrderBy(v => v.Id).Select(v => v.Id).ToListAsync(cancellationToken);
        var ciudadanoIds = await db.EventoCiudadanos.Where(c => c.EventoId == eventoId).OrderBy(c => c.Id).Select(c => c.Id).ToListAsync(cancellationToken);
        return new RegistrarEventoResult(eventoId, EsDuplicado: true, vehiculoIds, ciudadanoIds);
    }
}
