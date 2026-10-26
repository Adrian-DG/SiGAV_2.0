using Domain.Abstraction;
using Domain.Entities.Historico;
using Domain.Entities.Misc;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;
 

/// <summary>
/// Raíz del agregado de eventos (Asistencia en SiGAV 1.0). Las unidades, vehículos, personas,
/// tipos y evidencias solo se modifican a través de él, que garantiza sus invariantes.
/// </summary>
public class Evento : BaseEntityMetadata, IAuditableMetadata
{
    public const int DireccionMaxLength = 250;
    public const int ComentarioMaxLength = 2000;
    public const int MaxVehiculos = 30;
    public const int MaxCiudadanos = 30;
    public const int MaxEvidencias = 50;

    /// <summary>Tolerancia ante relojes de dispositivos adelantados.</summary>
    public static readonly TimeSpan ToleranciaReloj = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Clave de idempotencia enviada por la app: un reenvío de la cola offline con el mismo
    /// RequestId no crea un evento duplicado.
    /// </summary>
    public Guid? RequestId { get; private set; }

    public CanalReporteEnum CanalReporte { get; private set; }
    public EstadoEventoEnum Estado { get; private set; }
    /// <summary>Solo en eventos completados. La existencia del tipo la valida la aplicación.</summary>
    public int? TipoCierreId { get; private set; }
    public virtual TipoCierre? TipoCierre { get; private set; }
    public Coordenada Ubicacion { get; private set; } = null!;
    public string? Direccion { get; private set; }

    public int MunicipioId { get; private set; }
    public virtual Municipio? Municipio { get; private set; }

    /// <summary>
    /// Tramo donde ocurrió el evento (opcional). Las estadísticas lo atribuyen al tramo de la
    /// unidad; este dato permite también atribuirlo al lugar.
    /// </summary>
    public int? TramoId { get; private set; }
    public virtual Tramo? Tramo { get; private set; }

    public string? Comentario { get; private set; }

    /// <summary>Cuándo se reportó el evento (UTC). No confundir con CreatedAt, que es cuándo se guardó.</summary>
    public DateTime FechaHoraReporteUtc { get; private set; }
    public DateTime? FechaHoraLlegadaUtc { get; private set; }
    public DateTime? FechaHoraCompletadoUtc { get; private set; }

    private readonly List<EventoUnidadInfo> _unidades = new();
    private readonly List<EventoVehiculoInfo> _vehiculos = new();
    private readonly List<EventoCiudadanoInfo> _ciudadanos = new();
    private readonly List<EventoEvidencia> _evidencias = new();
    private readonly List<EventoTipoEvento> _tipos = new();

    public IReadOnlyCollection<EventoUnidadInfo> Unidades => _unidades.AsReadOnly();
    public IReadOnlyCollection<EventoVehiculoInfo> Vehiculos => _vehiculos.AsReadOnly();
    public IReadOnlyCollection<EventoCiudadanoInfo> Ciudadanos => _ciudadanos.AsReadOnly();
    public IReadOnlyCollection<EventoEvidencia> Evidencias => _evidencias.AsReadOnly();

    /// <summary>
    /// Tipos atendidos (un evento puede tener varios, como en SiGAV 1.0): una fila por tipo y por quién
    /// lo recibió, el evento en general o uno de sus vehículos o personas (<see cref="EventoVehiculoInfo.Tipos"/>,
    /// <see cref="EventoCiudadanoInfo.Tipos"/>). Un mismo tipo puede repetirse; para los tipos del evento
    /// use <see cref="TipoEventoIds"/>.
    /// </summary>
    public IReadOnlyCollection<EventoTipoEvento> Tipos => _tipos.AsReadOnly();

    /// <summary>Tipos del evento, sin repetir. Nunca vacío.</summary>
    public IReadOnlyCollection<int> TipoEventoIds => _tipos.Select(t => t.TipoEventoId).Distinct().ToList();

    public EventoUnidadInfo UnidadPrincipal => _unidades.Single(u => u.Rol == RolUnidadEventoEnum.Principal);

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private Evento() { }

    /// <summary>
    /// Registra un evento con su unidad principal. <paramref name="unidadPrincipal"/> debe traer
    /// su denominación cargada (se guarda como foto en la participación).
    /// </summary>
    public static Evento Registrar(
        Guid? requestId,
        CanalReporteEnum canalReporte,
        Coordenada ubicacion,
        int municipioId,
        int? tramoId,
        string? direccion,
        string? comentario,
        DateTime fechaHoraReporteUtc,
        IEnumerable<int> tipoEventoIds,
        Unidad unidadPrincipal,
        int agenteId,
        DateTime ahoraUtc)
    {
        if (requestId == Guid.Empty) throw new DomainException("La clave de idempotencia (RequestId) no es válida.");
        if (!Enum.IsDefined(canalReporte)) throw new DomainException("El canal de reporte no es válido.");
        ArgumentNullException.ThrowIfNull(ubicacion);

        var evento = new Evento
        {
            RequestId = requestId,
            CanalReporte = canalReporte,
            Estado = EstadoEventoEnum.Pendiente,
            FechaHoraReporteUtc = ValidarFecha(fechaHoraReporteUtc, ahoraUtc, "de reporte"),
            IsActive = true
        };

        evento.AsignarUbicacion(ubicacion, municipioId, tramoId, direccion);
        evento.Comentario = Normalizar(comentario, ComentarioMaxLength, "comentario");
        evento.AgregarTiposGenerales(tipoEventoIds);
        evento._unidades.Add(EventoUnidadInfo.Crear(unidadPrincipal, agenteId, RolUnidadEventoEnum.Principal));

        return evento;
    }

    // ---------------------------------------------------------------- Ciclo de vida

    /// <summary>La unidad llegó al lugar: Pendiente → En curso.</summary>
    public void IniciarAtencion(DateTime llegadaUtc, DateTime ahoraUtc)
    {
        AsegurarActivo();
        if (Estado != EstadoEventoEnum.Pendiente)
            throw new DomainException("Solo un evento pendiente puede pasar a en curso.");

        var llegada = ValidarFecha(llegadaUtc, ahoraUtc, "de llegada");
        if (llegada < FechaHoraReporteUtc)
            throw new DomainException("La llegada no puede ser anterior al reporte.");

        FechaHoraLlegadaUtc = llegada;
        Estado = EstadoEventoEnum.EnCurso;
    }

    /// <summary>
    /// Cierra el evento. Puede completarse sin haber estado en curso (p. ej. el ciudadano
    /// resolvió antes de que llegara la unidad).
    /// </summary>
    public void Completar(DateTime completadoUtc, int tipoCierreId, DateTime ahoraUtc)
    {
        AsegurarActivo();
        if (Estado == EstadoEventoEnum.Completado) throw new DomainException("El evento ya está completado.");
        if (tipoCierreId <= 0) throw new DomainException("El tipo de cierre no es válido.");

        var completado = ValidarFecha(completadoUtc, ahoraUtc, "de cierre");
        if (completado < (FechaHoraLlegadaUtc ?? FechaHoraReporteUtc))
            throw new DomainException("El cierre no puede ser anterior a la llegada ni al reporte.");

        FechaHoraCompletadoUtc = completado;
        // Si cambia el tipo, la navegación cargada deja de corresponder
        if (TipoCierre?.Id != tipoCierreId) TipoCierre = null;
        TipoCierreId = tipoCierreId;
        Estado = EstadoEventoEnum.Completado;
    }

    /// <summary>Anulación lógica (el "remove" de SiGAV 1.0): deja de contar en estadísticas.</summary>
    public void Anular() => IsActive = false;

    // ---------------------------------------------------------------- Datos

    /// <summary>
    /// Dónde ocurrió el evento. Se reemplaza completa: coordenada, municipio, tramo (opcional)
    /// y dirección de referencia. La existencia de municipio y tramo la valida la aplicación.
    /// </summary>
    public void AsignarUbicacion(Coordenada ubicacion, int municipioId, int? tramoId, string? direccion)
    {
        AsegurarActivo();
        ArgumentNullException.ThrowIfNull(ubicacion);
        if (municipioId <= 0) throw new DomainException("El municipio es requerido.");
        if (tramoId is <= 0) throw new DomainException("El tramo no es válido.");

        Ubicacion = ubicacion;
        // Si cambia el municipio o el tramo, la navegación cargada deja de corresponder
        if (Municipio?.Id != municipioId) Municipio = null;
        MunicipioId = municipioId;
        if (Tramo?.Id != tramoId) Tramo = null;
        TramoId = tramoId;
        Direccion = Normalizar(direccion, DireccionMaxLength, "dirección");
    }

    /// <summary>
    /// La unidad completa en campo un evento que le asignó el call center: se reemplazan ubicación,
    /// dirección, comentario y tipos, y se quitan vehículos y personas para volver a agregarlos con
    /// <see cref="AgregarVehiculo"/> y <see cref="AgregarCiudadano"/>. Las evidencias se conservan,
    /// sin la persona o el vehículo que tenían. <paramref name="requestId"/> es la clave con que la
    /// app reintenta el envío: queda como la del evento para reconocer el reenvío.
    /// </summary>
    public void ReemplazarDatos(
        Guid requestId,
        Coordenada ubicacion,
        int municipioId,
        int? tramoId,
        string? direccion,
        string? comentario,
        IEnumerable<int> tipoEventoIds)
    {
        AsegurarActivo();
        if (Estado == EstadoEventoEnum.Completado)
            throw new DomainException("El evento ya fue completado y no puede modificarse.");
        if (requestId == Guid.Empty) throw new DomainException("La clave de idempotencia (RequestId) no es válida.");

        AsignarUbicacion(ubicacion, municipioId, tramoId, direccion);
        Comentario = Normalizar(comentario, ComentarioMaxLength, "comentario");
        RequestId = requestId;

        foreach (var evidencia in _evidencias) evidencia.Desvincular();
        _tipos.Clear();
        _ciudadanos.Clear();
        _vehiculos.Clear();
        AgregarTiposGenerales(tipoEventoIds);
    }

    /// <summary>Agrega una unidad de apoyo (o un apoyo solicitado, como la unidad alfa).</summary>
    public void AgregarUnidadApoyo(Unidad unidad, int agenteId, bool yaLlego = true)
    {
        AsegurarActivo();
        if (_unidades.Any(u => u.UnidadId == unidad.Id))
            throw new DomainException($"La unidad '{unidad.Ficha}' ya participa en el evento.");

        _unidades.Add(EventoUnidadInfo.Crear(unidad, agenteId, yaLlego ? RolUnidadEventoEnum.Apoyo : RolUnidadEventoEnum.ApoyoSolicitado));
    }

    /// <summary>El apoyo solicitado llegó al lugar.</summary>
    public void ConfirmarLlegadaApoyo(int unidadId)
    {
        AsegurarActivo();
        var participacion = _unidades.FirstOrDefault(u => u.UnidadId == unidadId && u.Rol == RolUnidadEventoEnum.ApoyoSolicitado)
            ?? throw new DomainException("La unidad no tiene un apoyo solicitado en este evento.");

        participacion.CambiarRol(RolUnidadEventoEnum.Apoyo);
    }

    /// <summary>
    /// Cambia el agente que atiende el evento por parte de la unidad (p. ej. el componente se queda
    /// en el lugar mientras el otro agente va a otro evento). Un evento completado ya no cambia.
    /// </summary>
    public void CambiarAgenteUnidad(int unidadId, int agenteId)
    {
        AsegurarActivo();
        if (Estado == EstadoEventoEnum.Completado)
            throw new DomainException("No se puede cambiar el agente de un evento completado.");

        var participacion = _unidades.FirstOrDefault(u => u.UnidadId == unidadId)
            ?? throw new DomainException("La unidad no participa en este evento.");

        participacion.CambiarAgente(agenteId);
    }

    // ---------------------------------------------------------------- Vehículos y personas

    /// <summary>
    /// Agrega un vehículo involucrado. Devuelve la participación para asociarle personas con
    /// <see cref="AgregarCiudadano"/>. <paramref name="vehiculoHistorico"/> vincula el maestro
    /// cuando la placa ya es conocida; <paramref name="tipoEventoIds"/> son los tipos atendidos a
    /// este vehículo, de entre los del evento.
    /// </summary>
    public EventoVehiculoInfo AgregarVehiculo(
        DatosVehiculo datos,
        Vehiculo? vehiculoHistorico = null,
        IEnumerable<int>? tipoEventoIds = null)
    {
        AsegurarActivo();
        ArgumentNullException.ThrowIfNull(datos);
        if (_vehiculos.Count >= MaxVehiculos)
            throw new DomainException($"No se pueden registrar más de {MaxVehiculos} vehículos en un evento.");
        if (datos.Placa is { } placa && _vehiculos.Any(v => v.Datos.Placa == placa))
            throw new DomainException($"El vehículo con placa '{placa}' ya está registrado en el evento.");

        var tipos = TiposDelEvento(tipoEventoIds, "del vehículo");
        var vehiculo = EventoVehiculoInfo.Crear(datos, vehiculoHistorico);
        _vehiculos.Add(vehiculo);
        foreach (var id in tipos) vehiculo.AgregarTipo(AgregarTipo(id, vehiculo: vehiculo));
        return vehiculo;
    }

    /// <summary>
    /// Agrega una persona involucrada. Conductor y pasajero van en un <paramref name="vehiculo"/>
    /// de este evento (máximo un conductor por vehículo); un peatón, en ninguno.
    /// <paramref name="ciudadano"/> vincula el maestro cuando la persona ya existe;
    /// <paramref name="tipoEventoIds"/> son los tipos atendidos a esta persona, de entre los del evento.
    /// </summary>
    public EventoCiudadanoInfo AgregarCiudadano(
        RolCiudadanoEnum rol,
        DatosPersona persona,
        EventoVehiculoInfo? vehiculo = null,
        Ciudadano? ciudadano = null,
        IEnumerable<int>? tipoEventoIds = null)
    {
        AsegurarActivo();
        ArgumentNullException.ThrowIfNull(persona);
        if (_ciudadanos.Count >= MaxCiudadanos)
            throw new DomainException($"No se pueden registrar más de {MaxCiudadanos} personas en un evento.");

        if (persona.Identificacion is { } identificacion
            && _ciudadanos.Any(c => c.Persona.Identificacion == identificacion))
            throw new DomainException($"La persona '{identificacion}' ya está registrada en el evento.");

        if (vehiculo is not null)
        {
            if (!_vehiculos.Contains(vehiculo))
                throw new DomainException("El vehículo de la persona no está registrado en este evento.");
            if (rol == RolCiudadanoEnum.Conductor
                && _ciudadanos.Any(c => c.Rol == RolCiudadanoEnum.Conductor && ReferenceEquals(c.Vehiculo, vehiculo)))
                throw new DomainException($"El vehículo {vehiculo.Datos.Placa ?? "sin placa"} ya tiene un conductor.");
        }

        var tipos = TiposDelEvento(tipoEventoIds, "de la persona");
        var participacion = EventoCiudadanoInfo.Crear(rol, persona, vehiculo, ciudadano);
        _ciudadanos.Add(participacion);
        foreach (var id in tipos) participacion.AgregarTipo(AgregarTipo(id, ciudadano: participacion));
        return participacion;
    }

    /// <summary>
    /// Agrega una foto o firma ya guardada en el almacenamiento (<paramref name="ubicacion"/> es su
    /// clave). Requiere los ciudadanos, vehículos y evidencias cargados: la persona o el vehículo
    /// indicados deben ser de este evento.
    /// </summary>
    public EventoEvidencia AgregarEvidencia(
        TipoEvidenciaEnum tipo,
        string ubicacion,
        string contentType,
        long tamanoBytes,
        DateTime registradaUtc,
        Guid? requestId = null,
        int? eventoCiudadanoId = null,
        int? eventoVehiculoId = null)
    {
        AsegurarActivo();
        if (_evidencias.Count >= MaxEvidencias)
            throw new DomainException($"No se pueden registrar más de {MaxEvidencias} evidencias en un evento.");

        EventoCiudadanoInfo? ciudadano = null;
        if (eventoCiudadanoId is { } ciudadanoId)
            ciudadano = _ciudadanos.FirstOrDefault(c => c.Id == ciudadanoId)
                ?? throw new DomainException("La persona indicada no está registrada en este evento.");

        EventoVehiculoInfo? vehiculo = null;
        if (eventoVehiculoId is { } vehiculoId)
            vehiculo = _vehiculos.FirstOrDefault(v => v.Id == vehiculoId)
                ?? throw new DomainException("El vehículo indicado no está registrado en este evento.");

        var evidencia = EventoEvidencia.Crear(tipo, ubicacion, contentType, tamanoBytes, registradaUtc, requestId, ciudadano, vehiculo);
        _evidencias.Add(evidencia);
        return evidencia;
    }

    // ---------------------------------------------------------------- Reglas internas

    /// <summary>Tipos con que se registra el evento (sin duplicados). Al menos uno.</summary>
    private void AgregarTiposGenerales(IEnumerable<int> tipoEventoIds)
    {
        var ids = (tipoEventoIds ?? []).Distinct().ToList();
        if (ids.Count == 0) throw new DomainException("El evento debe tener al menos un tipo de evento.");
        if (ids.Any(id => id <= 0)) throw new DomainException("Hay tipos de evento no válidos.");

        foreach (var id in ids) AgregarTipo(id);
    }

    /// <summary>Tipos de un vehículo o persona, sin duplicados: deben ser tipos de este evento.</summary>
    private List<int> TiposDelEvento(IEnumerable<int>? tipoEventoIds, string de)
    {
        var ids = (tipoEventoIds ?? []).Distinct().ToList();
        var delEvento = TipoEventoIds;
        if (ids.Any(id => !delEvento.Contains(id)))
            throw new DomainException($"Los tipos {de} deben estar entre los tipos del evento.");
        return ids;
    }

    /// <summary>
    /// Agrega la fila del tipo. Si es de un vehículo o persona, la fila general del mismo tipo sobra
    /// (el tipo sigue siendo del evento a través de ellos) y se quita.
    /// </summary>
    private EventoTipoEvento AgregarTipo(int tipoEventoId, EventoVehiculoInfo? vehiculo = null, EventoCiudadanoInfo? ciudadano = null)
    {
        if (vehiculo is not null || ciudadano is not null)
            _tipos.RemoveAll(t => t.EsGeneral && t.TipoEventoId == tipoEventoId);

        var tipo = new EventoTipoEvento(tipoEventoId, vehiculo, ciudadano);
        _tipos.Add(tipo);
        return tipo;
    }

    private void AsegurarActivo()
    {
        if (!IsActive) throw new DomainException("El evento está anulado.");
    }

    private static DateTime ValidarFecha(DateTime fecha, DateTime ahoraUtc, string descripcion)
    {
        if (fecha.Kind == DateTimeKind.Local) fecha = fecha.ToUniversalTime();
        var utc = DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

        if (utc == default) throw new DomainException($"La fecha {descripcion} es requerida.");
        if (utc > ahoraUtc + ToleranciaReloj) throw new DomainException($"La fecha {descripcion} no puede estar en el futuro.");

        return utc;
    }
    private static string? Normalizar(string? valor, int maxLength, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        var normalizado = valor.Trim();
        if (normalizado.Length > maxLength)
            throw new DomainException($"El {campo} no puede exceder {maxLength} caracteres.");

        return normalizado;
    }
}
