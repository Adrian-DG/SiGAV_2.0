using Domain.Abstraction;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

public class Unidad : BaseEntityMetadata, IAuditableMetadata
{
    public const int FichaMaxLength = 20;
    public const int PlacaMaxLength = 10;

    public string Ficha { get; private set; } = null!;
    public string? Placa { get; private set; }

    /// <summary>
    /// Indica si la unidad está operativa (puede iniciar sesión en la app / recibir asistencias).
    /// </summary>
    public bool EstaDisponible { get; private set; }

    /// <summary>
    /// Una unidad puede quedar sin denominación cuando otra unidad toma la suya.
    /// </summary>
    public int? DenominacionId { get; private set; }
    public virtual Denominacion? Denominacion { get; private set; }

    // Solo se agregan registros; no se carga al leer la unidad (se consulta aparte)
    private readonly List<HistorialDenominacionUnidad> _historialDenominaciones = [];
    public IReadOnlyCollection<HistorialDenominacionUnidad> HistorialDenominaciones => _historialDenominaciones.AsReadOnly();

    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private Unidad() { }

    /// <summary>
    /// Crea la unidad sin denominación; la asignación se realiza a través de
    /// <see cref="Services.AsignacionDenominacionService"/> para respetar la exclusividad.
    /// </summary>
    public static Unidad Crear(string ficha, string? placa = null)
    {
        return new Unidad
        {
            Ficha = NormalizarFicha(ficha),
            Placa = NormalizarPlaca(placa),
            EstaDisponible = false,
            IsActive = true
        };
    }

    public void ActualizarDatos(string ficha, string? placa)
    {
        AsegurarActiva();
        Ficha = NormalizarFicha(ficha);
        Placa = NormalizarPlaca(placa);
    }

    /// <summary>
    /// Asigna la denominación y deja el registro de auditoría. Si ya la tenía solo vuelve a
    /// marcar la unidad como disponible (no hay cambio que auditar).
    /// </summary>
    internal void AsignarDenominacion(Denominacion denominacion, AutorCambio autor, Unidad? unidadDesplazada)
    {
        AsegurarActiva();
        if (!denominacion.IsActive) throw new DomainException("No se puede asignar una denominación inactiva.");

        var yaLaTenia = ReferenceEquals(Denominacion, denominacion) || (denominacion.Id > 0 && DenominacionId == denominacion.Id);
        if (!yaLaTenia)
        {
            var tipo = DenominacionId is null ? TipoCambioDenominacionEnum.Asignacion : TipoCambioDenominacionEnum.Reasignacion;
            _historialDenominaciones.Add(HistorialDenominacionUnidad.Registrar(tipo, DenominacionId, denominacion, unidadDesplazada, autor));
        }

        Denominacion = denominacion;
        // Si la denominación es nueva (Id = 0), EF Core resuelve la FK a partir de la navegación al guardar.
        DenominacionId = denominacion.Id > 0 ? denominacion.Id : null;
        EstaDisponible = true;
    }

    /// <summary>La unidad pierde su denominación porque <paramref name="unidadQueLaToma"/> la ocupa.</summary>
    internal void LiberarDenominacion(AutorCambio autor, Unidad unidadQueLaToma)
    {
        if (DenominacionId is null && Denominacion is null) return;

        _historialDenominaciones.Add(HistorialDenominacionUnidad.Registrar(
            TipoCambioDenominacionEnum.Liberacion, DenominacionId ?? Denominacion?.Id, null, unidadQueLaToma, autor));

        Denominacion = null;
        DenominacionId = null;
        EstaDisponible = false;
    }

    public void AlternarDisponibilidad()
    {
        AsegurarActiva();
        EstaDisponible = !EstaDisponible;
    }

    /// <summary>
    /// Desactivación lógica: la unidad ya no podrá iniciar sesión ni ser reasignada.
    /// </summary>
    public void Desactivar()
    {
        IsActive = false;
        EstaDisponible = false;
    }

    public bool TieneDenominacion(int denominacionId) => DenominacionId == denominacionId;

    private void AsegurarActiva()
    {
        if (!IsActive) throw new DomainException($"La unidad '{Ficha}' está desactivada.");
    }

    public static string NormalizarFicha(string ficha)
    {
        if (string.IsNullOrWhiteSpace(ficha)) throw new DomainException("La ficha de la unidad es requerida.");

        var normalizada = ficha.Trim();
        if (normalizada.Length > FichaMaxLength)
            throw new DomainException($"La ficha no puede exceder {FichaMaxLength} caracteres.");

        return normalizada;
    }

    private static string? NormalizarPlaca(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return null;

        var normalizada = placa.Trim().ToUpperInvariant();
        if (normalizada.Length > PlacaMaxLength)
            throw new DomainException($"La placa no puede exceder {PlacaMaxLength} caracteres.");

        return normalizada;
    }
}
