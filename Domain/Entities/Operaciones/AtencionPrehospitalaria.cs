using Domain.Entities.Misc;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Atención prehospitalaria (ambulancia) a un paciente del evento: AsistenciaPreHospitalaria en
/// SiGAV 1.0, que era un registro aparte sin vínculo con el evento. Aquí pertenece a la persona del
/// evento que atendió la ambulancia, con cualquier rol (sus datos personales son los de <see cref="EventoCiudadanoInfo.Persona"/>)
/// y solo se modifica a través del agregado <see cref="Evento"/>.
/// </summary>
public class AtencionPrehospitalaria
{
    public const int TextoCortoMaxLength = 150;
    public const int TextoLargoMaxLength = 2000;
    public const int MaxTomasSignosVitales = 20;

    /// <summary>
    /// Clave con que la app identifica esta atención (la genera sin conexión). Un reenvío con la misma
    /// clave actualiza la atención en lugar de crear otra persona, aunque el paciente sea desconocido.
    /// </summary>
    public Guid? RequestId { get; private set; }

    public DespachoPrehospitalarioEnum DespachadoPor { get; private set; }
    public TipoAtencionPrehospitalariaEnum TipoAtencion { get; private set; }
    /// <summary>Solo en <see cref="TipoAtencionPrehospitalariaEnum.EventoEspecial"/>.</summary>
    public string? NombreEventoEspecial { get; private set; }
    public CausaAtencionPrehospitalariaEnum Causa { get; private set; }

    // ---- Traslado
    public bool FueTrasladado { get; private set; }
    /// <summary>Centro que recibió al paciente (solo si fue trasladado). La existencia la valida la aplicación.</summary>
    public int? HospitalId { get; private set; }
    public virtual Hospital? Hospital { get; private set; }
    /// <summary>Quién recibió al paciente en el centro de salud.</summary>
    public string? RecibidoPor { get; private set; }
    /// <summary>Solo si no fue trasladado.</summary>
    public MotivoNoTrasladoEnum? MotivoNoTraslado { get; private set; }

    // ---- Clínico
    /// <summary>Antecedentes mórbidos; null = desconocidos.</summary>
    public string? Antecedentes { get; private set; }
    public string? HallazgosPositivos { get; private set; }
    public string? DiagnosticoPresuntivo { get; private set; }
    public string? ProcedimientosRealizados { get; private set; }
    /// <summary>Insumos y medicamentos utilizados.</summary>
    public string? InsumosUtilizados { get; private set; }

    // ---- Personal (agentes del área PreHospitalaria; lo valida la aplicación)
    public int? MedicoAgenteId { get; private set; }
    public virtual Agente? Medico { get; private set; }
    /// <summary>Regulador de la emergencia que coordinó la atención.</summary>
    public int? ReguladorAgenteId { get; private set; }
    public virtual Agente? Regulador { get; private set; }

    private readonly List<TomaSignosVitales> _signosVitales = new();

    /// <summary>Tomas de signos vitales, en orden cronológico.</summary>
    public IReadOnlyCollection<TomaSignosVitales> SignosVitales => _signosVitales.AsReadOnly();

    // Requerido por EF Core
    private AtencionPrehospitalaria() { }

    public static AtencionPrehospitalaria Registrar(
        DespachoPrehospitalarioEnum despachadoPor,
        TipoAtencionPrehospitalariaEnum tipoAtencion,
        CausaAtencionPrehospitalariaEnum causa,
        bool fueTrasladado,
        int? hospitalId = null,
        string? recibidoPor = null,
        MotivoNoTrasladoEnum? motivoNoTraslado = null,
        string? nombreEventoEspecial = null,
        string? antecedentes = null,
        string? hallazgosPositivos = null,
        string? diagnosticoPresuntivo = null,
        string? procedimientosRealizados = null,
        string? insumosUtilizados = null,
        int? medicoAgenteId = null,
        int? reguladorAgenteId = null,
        IEnumerable<TomaSignosVitales>? signosVitales = null,
        Guid? requestId = null)
    {
        if (requestId == Guid.Empty) throw new DomainException("La clave de la atención (requestId) no es válida.");
        if (!Enum.IsDefined(despachadoPor)) throw new DomainException("Quién despachó la ambulancia no es válido.");
        if (!Enum.IsDefined(tipoAtencion)) throw new DomainException("El tipo de atención no es válido.");
        if (!Enum.IsDefined(causa)) throw new DomainException("La causa de la atención no es válida.");
        if (medicoAgenteId is <= 0) throw new DomainException("El médico no es válido.");
        if (reguladorAgenteId is <= 0) throw new DomainException("El regulador no es válido.");

        var atencion = new AtencionPrehospitalaria
        {
            RequestId = requestId,
            DespachadoPor = despachadoPor,
            TipoAtencion = tipoAtencion,
            Causa = causa,
            Antecedentes = Normalizar(antecedentes, TextoLargoMaxLength, "antecedentes"),
            HallazgosPositivos = Normalizar(hallazgosPositivos, TextoLargoMaxLength, "hallazgos positivos"),
            DiagnosticoPresuntivo = Normalizar(diagnosticoPresuntivo, TextoLargoMaxLength, "diagnóstico presuntivo"),
            ProcedimientosRealizados = Normalizar(procedimientosRealizados, TextoLargoMaxLength, "procedimientos realizados"),
            InsumosUtilizados = Normalizar(insumosUtilizados, TextoLargoMaxLength, "insumos utilizados"),
            MedicoAgenteId = medicoAgenteId,
            ReguladorAgenteId = reguladorAgenteId
        };

        var nombreEvento = Normalizar(nombreEventoEspecial, TextoCortoMaxLength, "nombre del evento especial");
        if (tipoAtencion == TipoAtencionPrehospitalariaEnum.EventoEspecial)
            atencion.NombreEventoEspecial = nombreEvento
                ?? throw new DomainException("Indique el nombre del evento especial.");

        atencion.AsignarTraslado(fueTrasladado, hospitalId, recibidoPor, motivoNoTraslado);

        var tomas = (signosVitales ?? []).OrderBy(t => t.TomadaUtc).ToList();
        if (tomas.Count > MaxTomasSignosVitales)
            throw new DomainException($"No se pueden registrar más de {MaxTomasSignosVitales} tomas de signos vitales.");
        atencion._signosVitales.AddRange(tomas);

        return atencion;
    }

    /// <summary>Con traslado: centro de salud obligatorio y sin motivo de no traslado; sin traslado, al revés.</summary>
    private void AsignarTraslado(bool fueTrasladado, int? hospitalId, string? recibidoPor, MotivoNoTrasladoEnum? motivoNoTraslado)
    {
        FueTrasladado = fueTrasladado;
        if (fueTrasladado)
        {
            if (hospitalId is not > 0) throw new DomainException("Indique el centro de salud al que se trasladó al paciente.");
            if (motivoNoTraslado is not null) throw new DomainException("Un paciente trasladado no tiene motivo de no traslado.");
            HospitalId = hospitalId;
            RecibidoPor = Normalizar(recibidoPor, TextoCortoMaxLength, "nombre de quien recibió al paciente");
            MotivoNoTraslado = null;
            return;
        }

        if (motivoNoTraslado is not { } motivo || !Enum.IsDefined(motivo))
            throw new DomainException("Indique por qué el paciente no fue trasladado.");
        if (hospitalId is not null) throw new DomainException("Un paciente no trasladado no tiene centro de salud.");
        HospitalId = null;
        RecibidoPor = null;
        MotivoNoTraslado = motivo;
    }

    private static string? Normalizar(string? valor, int maxLength, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        var normalizado = valor.Trim();
        if (normalizado.Length > maxLength)
            throw new DomainException($"El campo {campo} no puede exceder {maxLength} caracteres.");

        return normalizado;
    }
}
