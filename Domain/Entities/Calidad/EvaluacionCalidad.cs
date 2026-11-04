using Domain.Abstraction;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Calidad;

/// <summary>
/// Evaluación de calidad de un evento completado: se contacta a una de sus personas y se le hace la
/// encuesta vigente. Una por evento. No depende del canal: un evaluador por llamada o, más adelante,
/// un bot de mensajería registran los mismos intentos y respuestas (las respuestas se pueden guardar
/// de a una).
/// </summary>
public class EvaluacionCalidad : BaseEntityMetadata, IAuditableMetadata
{
    public const int TelefonoMinLength = 7;
    public const int TelefonoMaxLength = 20;
    public const int NombreContactoMaxLength = 101;
    public const int ObservacionMaxLength = 500;

    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    /// <summary>Versión de la encuesta con que se evalúa (la vigente al iniciar).</summary>
    public int EncuestaId { get; private set; }
    public virtual Encuesta? Encuesta { get; private set; }

    public EstadoEvaluacionEnum Estado { get; private set; }
    public CanalContactoEnum Canal { get; private set; }

    /// <summary>Persona del evento a la que se contacta. null = otro contacto (p. ej. un familiar).</summary>
    public int? EventoCiudadanoId { get; private set; }
    public virtual EventoCiudadanoInfo? EventoCiudadano { get; private set; }

    /// <summary>Copia del nombre y teléfono usados: pueden diferir de los del evento.</summary>
    public string? NombreContacto { get; private set; }
    public string TelefonoContacto { get; private set; } = null!;

    /// <summary>Observación general (p. ej. por qué no se pudo contactar).</summary>
    public string? Observacion { get; private set; }

    /// <summary>Cuándo se cerró (completada, no contactado o rechazada). null mientras está en gestión.</summary>
    public DateTime? CerradaUtc { get; private set; }

    /// <summary>Usuario web que la cerró. null = la cerró el bot.</summary>
    public int? CerradaPorUsuarioId { get; private set; }

    private readonly List<IntentoContacto> _intentos = new();
    private readonly List<RespuestaEvaluacion> _respuestas = new();

    public IReadOnlyCollection<IntentoContacto> Intentos => _intentos.AsReadOnly();
    public IReadOnlyCollection<RespuestaEvaluacion> Respuestas => _respuestas.AsReadOnly();

    public bool Contesto => _intentos.Any(i => i.Resultado == ResultadoContactoEnum.Contesto);

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private EvaluacionCalidad() { }

    /// <summary>
    /// Inicia la evaluación de <paramref name="evento"/> (con sus personas cargadas) usando la
    /// encuesta vigente (con sus preguntas cargadas).
    /// </summary>
    public static EvaluacionCalidad Iniciar(
        Evento evento,
        Encuesta encuesta,
        CanalContactoEnum canal,
        int? eventoCiudadanoId,
        string? telefono,
        string? nombreContacto)
    {
        ArgumentNullException.ThrowIfNull(evento);
        ArgumentNullException.ThrowIfNull(encuesta);
        if (!evento.IsActive) throw new DomainException("El evento fue anulado: no se evalúa.");
        if (evento.Estado != EstadoEventoEnum.Completado) throw new DomainException("Solo se evalúan eventos completados.");
        if (encuesta.Estado != EstadoEncuestaEnum.Publicada) throw new DomainException("La encuesta no es la vigente.");
        if (!Enum.IsDefined(canal)) throw new DomainException("El canal de contacto no es válido.");

        var evaluacion = new EvaluacionCalidad
        {
            EventoId = evento.Id,
            Evento = evento,
            EncuestaId = encuesta.Id,
            Encuesta = encuesta,
            Canal = canal,
            Estado = EstadoEvaluacionEnum.EnGestion,
            IsActive = true
        };
        evaluacion.DefinirContacto(evento, eventoCiudadanoId, telefono, nombreContacto);
        return evaluacion;
    }

    /// <summary>Cambia a quién se contacta (p. ej. el número no era correcto). Solo en gestión.</summary>
    public void CambiarContacto(Evento evento, int? eventoCiudadanoId, string? telefono, string? nombreContacto)
    {
        AsegurarEnGestion();
        if (evento.Id != EventoId) throw new DomainException("El evento no corresponde a la evaluación.");
        DefinirContacto(evento, eventoCiudadanoId, telefono, nombreContacto);
    }

    /// <summary>
    /// Registra un intento de contacto. Un rechazo cierra la evaluación. Devuelve false si el
    /// intento ya estaba registrado (misma <paramref name="referenciaExterna"/>: reenvío de un webhook).
    /// </summary>
    public bool RegistrarIntento(
        CanalContactoEnum canal,
        ResultadoContactoEnum resultado,
        int? usuarioId,
        string? observacion,
        DateTime fechaUtc,
        string? referenciaExterna = null)
    {
        var intento = IntentoContacto.Crear(canal, resultado, usuarioId, observacion, fechaUtc, referenciaExterna);
        if (intento.ReferenciaExterna is not null && _intentos.Any(i => i.ReferenciaExterna == intento.ReferenciaExterna))
            return false;

        AsegurarEnGestion();
        _intentos.Add(intento);

        if (resultado == ResultadoContactoEnum.RechazoEncuesta)
            Cerrar(EstadoEvaluacionEnum.Rechazada, usuarioId, fechaUtc);

        return true;
    }

    /// <summary>
    /// Guarda (o reemplaza) la respuesta a una pregunta de la encuesta de esta evaluación. Sin valor
    /// ni texto la pregunta queda sin responder. Requiere que el ciudadano haya contestado.
    /// </summary>
    public void Responder(int preguntaId, int? valor, string? texto, string? comentario)
    {
        AsegurarEnGestion();
        if (!Contesto) throw new DomainException("Registre primero el intento en que el ciudadano contestó.");
        var encuesta = Encuesta ?? throw new InvalidOperationException("La encuesta de la evaluación no está cargada.");

        var pregunta = encuesta.Pregunta(preguntaId);
        var respuesta = pregunta.Normalizar(valor, texto, comentario);
        var actual = _respuestas.FirstOrDefault(r => r.PreguntaEncuestaId == preguntaId);

        if (respuesta is null)
        {
            if (actual is not null) _respuestas.Remove(actual);
            return;
        }

        if (actual is null) _respuestas.Add(RespuestaEvaluacion.Crear(preguntaId, respuesta.Value.Valor, respuesta.Value.Texto, respuesta.Value.Comentario));
        else actual.Cambiar(respuesta.Value.Valor, respuesta.Value.Texto, respuesta.Value.Comentario);
    }

    /// <summary>Siguiente pregunta sin responder, en orden (la que haría un bot), o null si no queda ninguna.</summary>
    public PreguntaEncuesta? SiguientePregunta()
    {
        var encuesta = Encuesta ?? throw new InvalidOperationException("La encuesta de la evaluación no está cargada.");
        var respondidas = _respuestas.Select(r => r.PreguntaEncuestaId).ToHashSet();
        return encuesta.Preguntas.FirstOrDefault(p => !respondidas.Contains(p.Id));
    }

    public void Completar(int? usuarioId, DateTime ahoraUtc, string? observacion = null)
    {
        AsegurarEnGestion();
        if (!Contesto) throw new DomainException("No se puede completar: el ciudadano no ha contestado.");
        var encuesta = Encuesta ?? throw new InvalidOperationException("La encuesta de la evaluación no está cargada.");

        var respondidas = _respuestas.Select(r => r.PreguntaEncuestaId).ToHashSet();
        var faltantes = encuesta.Preguntas.Where(p => p.Obligatoria && !respondidas.Contains(p.Id)).Select(p => p.Orden).ToList();
        if (faltantes.Count > 0)
            throw new DomainException($"Faltan preguntas obligatorias: {string.Join(", ", faltantes)}.");

        Observacion = Normalizar(observacion) ?? Observacion;
        Cerrar(EstadoEvaluacionEnum.Completada, usuarioId, ahoraUtc);
    }

    /// <summary>Cierra sin encuesta porque no se logró contactar. Requiere al menos un intento fallido.</summary>
    public void MarcarNoContactado(int? usuarioId, DateTime ahoraUtc, string? observacion)
    {
        AsegurarEnGestion();
        if (_intentos.Count == 0) throw new DomainException("Registre al menos un intento de contacto.");
        if (Contesto) throw new DomainException("El ciudadano ya contestó: complete la encuesta o registre que la rechazó.");

        Observacion = Normalizar(observacion) ?? Observacion;
        _respuestas.Clear();
        Cerrar(EstadoEvaluacionEnum.NoContactado, usuarioId, ahoraUtc);
    }

    /// <summary>Vuelve a gestión una evaluación cerrada (para corregirla o reintentar el contacto).</summary>
    public void Reabrir()
    {
        if (Estado == EstadoEvaluacionEnum.EnGestion) throw new DomainException("La evaluación ya está en gestión.");
        Estado = EstadoEvaluacionEnum.EnGestion;
        CerradaUtc = null;
        CerradaPorUsuarioId = null;
    }

    private void Cerrar(EstadoEvaluacionEnum estado, int? usuarioId, DateTime ahoraUtc)
    {
        Estado = estado;
        CerradaUtc = ahoraUtc;
        CerradaPorUsuarioId = usuarioId;
    }

    private void DefinirContacto(Evento evento, int? eventoCiudadanoId, string? telefono, string? nombreContacto)
    {
        EventoCiudadanoInfo? persona = null;
        if (eventoCiudadanoId is { } id)
            persona = evento.Ciudadanos.FirstOrDefault(c => c.Id == id)
                ?? throw new DomainException("La persona indicada no participa en el evento.");

        var numero = NormalizarTelefono(telefono ?? persona?.Persona.Telefono)
            ?? throw new DomainException("Indique un teléfono de contacto.");

        var nombre = nombreContacto?.Trim();
        if (string.IsNullOrEmpty(nombre) && persona is not null)
            nombre = $"{persona.Persona.Nombre} {persona.Persona.Apellido}".Trim();
        if (nombre?.Length > NombreContactoMaxLength)
            throw new DomainException($"El nombre de contacto no puede exceder {NombreContactoMaxLength} caracteres.");

        EventoCiudadanoId = persona?.Id;
        EventoCiudadano = persona;
        TelefonoContacto = numero;
        NombreContacto = string.IsNullOrEmpty(nombre) ? null : nombre;
    }

    /// <summary>Solo dígitos (y un "+" inicial). null si no hay número.</summary>
    public static string? NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;
        var limpio = telefono.Trim();
        var digitos = new string(limpio.Where(char.IsDigit).ToArray());
        if (digitos.Length < TelefonoMinLength || digitos.Length > TelefonoMaxLength - 1)
            throw new DomainException($"El teléfono debe tener entre {TelefonoMinLength} y {TelefonoMaxLength - 1} dígitos.");
        return limpio.StartsWith('+') ? "+" + digitos : digitos;
    }

    private void AsegurarEnGestion()
    {
        if (Estado != EstadoEvaluacionEnum.EnGestion)
            throw new DomainException("La evaluación está cerrada; reábrala para modificarla.");
    }

    private static string? Normalizar(string? observacion)
    {
        var limpio = observacion?.Trim();
        if (string.IsNullOrEmpty(limpio)) return null;
        if (limpio.Length > ObservacionMaxLength)
            throw new DomainException($"La observación no puede exceder {ObservacionMaxLength} caracteres.");
        return limpio;
    }
}
