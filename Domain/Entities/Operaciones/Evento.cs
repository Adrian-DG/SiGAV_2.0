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

    /// <summary>Tipos atendidos (un evento puede tener varios, como en SiGAV 1.0). Nunca vacío.</summary>
    public IReadOnlyCollection<EventoTipoEvento> Tipos => _tipos.AsReadOnly();

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
        evento.ReemplazarTipos(tipoEventoIds);
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
    /// Deja exactamente los tipos indicados (sin duplicados). Conserva los que ya tenía y solo
    /// agrega o quita la diferencia, así la persistencia no borra y reinserta las mismas filas.
    /// </summary>
    public void ReemplazarTipos(IEnumerable<int> tipoEventoIds)
    {
        AsegurarActivo();

        var ids = (tipoEventoIds ?? []).Distinct().ToList();
        if (ids.Count == 0) throw new DomainException("El evento debe tener al menos un tipo de evento.");
        if (ids.Any(id => id <= 0)) throw new DomainException("Hay tipos de evento no válidos.");

        _tipos.RemoveAll(t => !ids.Contains(t.TipoEventoId));
        foreach (var id in ids.Where(id => _tipos.All(t => t.TipoEventoId != id)))
            _tipos.Add(new EventoTipoEvento(id));
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

    // ---------------------------------------------------------------- Vehículos y personas

    /// <summary>
    /// Agrega un vehículo involucrado. Devuelve la participación para asociarle personas con
    /// <see cref="AgregarCiudadano"/>. <paramref name="vehiculoHistorico"/> vincula el maestro
    /// cuando la placa ya es conocida.
    /// </summary>
    public EventoVehiculoInfo AgregarVehiculo(DatosVehiculo datos, Vehiculo? vehiculoHistorico = null)
    {
        AsegurarActivo();
        ArgumentNullException.ThrowIfNull(datos);
        if (_vehiculos.Count >= MaxVehiculos)
            throw new DomainException($"No se pueden registrar más de {MaxVehiculos} vehículos en un evento.");
        if (datos.Placa is { } placa && _vehiculos.Any(v => v.Datos.Placa == placa))
            throw new DomainException($"El vehículo con placa '{placa}' ya está registrado en el evento.");

        var vehiculo = EventoVehiculoInfo.Crear(datos, vehiculoHistorico);
        _vehiculos.Add(vehiculo);
        return vehiculo;
    }

    /// <summary>
    /// Agrega una persona involucrada. Conductor y pasajero van en un <paramref name="vehiculo"/>
    /// de este evento (máximo un conductor por vehículo); un peatón, en ninguno.
    /// <paramref name="ciudadano"/> vincula el maestro cuando la persona ya existe.
    /// </summary>
    public void AgregarCiudadano(
        RolCiudadanoEnum rol,
        DatosPersona persona,
        EventoVehiculoInfo? vehiculo = null,
        Ciudadano? ciudadano = null)
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

        _ciudadanos.Add(EventoCiudadanoInfo.Crear(rol, persona, vehiculo, ciudadano));
    }

    // ---------------------------------------------------------------- Reglas internas

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
