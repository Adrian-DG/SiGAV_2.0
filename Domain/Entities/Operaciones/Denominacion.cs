using Domain.Abstraction;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

public class Denominacion : NamedMetadata, IAuditableMetadata
{
    public const int NombreMaxLength = 100;

    public int TramoId { get; private set; }
    public virtual Tramo? Tramo { get; private set; }

    public int NivelDenominacionId { get; private set; }
    public virtual NivelDenominacion? Nivel { get; private set; }

    private readonly List<DenominacionRegion> _regiones = [];
    private readonly List<DenominacionTramo> _tramos = [];

    /// <summary>Regiones supervisadas (solo jerarquía Regional).</summary>
    public IReadOnlyCollection<DenominacionRegion> Regiones => _regiones.AsReadOnly();

    /// <summary>Tramos adicionales a <see cref="TramoId"/> (solo jerarquía Tramo).</summary>
    public IReadOnlyCollection<DenominacionTramo> Tramos => _tramos.AsReadOnly();

    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private Denominacion() { }

    public static Denominacion Crear(string nombre, int tramoId, int nivelDenominacionId)
    {
        var nombreNormalizado = NormalizarNombre(nombre);

        if (tramoId <= 0) throw new DomainException("La denominación debe pertenecer a un tramo válido.");
        if (nivelDenominacionId <= 0) throw new DomainException("La denominación debe tener un nivel válido.");

        return new Denominacion
        {
            Nombre = nombreNormalizado,
            TramoId = tramoId,
            NivelDenominacionId = nivelDenominacionId,
            IsActive = true
        };
    }

    /// <summary>
    /// Cambia el nivel. Si cambia la jerarquía, las asignaciones de regiones o tramos dejan de
    /// tener sentido y se descartan.
    /// </summary>
    public void CambiarNivel(NivelDenominacion nivel)
    {
        if (nivel.Id <= 0 || !nivel.IsActive) throw new DomainException("El nivel de denominación no es válido.");
        if (nivel.Id == NivelDenominacionId) return;

        if (Nivel is null || Nivel.Jerarquia != nivel.Jerarquia)
        {
            _regiones.Clear();
            _tramos.Clear();
        }

        Nivel = nivel;
        NivelDenominacionId = nivel.Id;
    }

    /// <summary>
    /// Reemplaza las asignaciones de supervisión. Requiere que <see cref="Nivel"/> esté cargado.
    /// </summary>
    public void DefinirAsignaciones(
        IEnumerable<RegionMacroEnum> regionesMacro,
        IEnumerable<int> regionesAsistencia,
        IEnumerable<int> tramos)
    {
        var jerarquia = ObtenerJerarquia();
        var macros = regionesMacro.Distinct().ToList();
        var regiones = regionesAsistencia.Distinct().ToList();
        var tramosAdicionales = tramos.Distinct().Where(t => t != TramoId).ToList();

        if (macros.Any(m => !Enum.IsDefined(m)))
            throw new DomainException("Hay regiones macro no válidas.");

        if (jerarquia != JerarquiaEnum.Regional && (macros.Count > 0 || regiones.Count > 0))
            throw new DomainException($"La denominación '{Nombre}' no es de nivel regional; no puede tener regiones asignadas.");

        if (jerarquia != JerarquiaEnum.Tramo && tramosAdicionales.Count > 0)
            throw new DomainException($"La denominación '{Nombre}' no es de nivel tramo; no puede tener tramos asignados.");

        _regiones.Clear();
        _regiones.AddRange(macros.Select(DenominacionRegion.DeMacro));
        _regiones.AddRange(regiones.Select(DenominacionRegion.DeAsistencia));

        _tramos.Clear();
        _tramos.AddRange(tramosAdicionales.Select(t => new DenominacionTramo(t)));
    }

    /// <summary>
    /// Qué eventos puede ver en las estadísticas la unidad que opera esta denominación.
    /// Requiere que <see cref="Nivel"/> y las asignaciones estén cargados.
    /// </summary>
    public AlcanceEstadistico ObtenerAlcance(int unidadId) => ObtenerJerarquia() switch
    {
        JerarquiaEnum.Regional => AlcanceEstadistico.Regional(
            Id,
            _regiones.Where(r => r.RegionMacro.HasValue).Select(r => r.RegionMacro!.Value),
            _regiones.Where(r => r.RegionAsistenciaId.HasValue).Select(r => r.RegionAsistenciaId!.Value)),
        JerarquiaEnum.Tramo => AlcanceEstadistico.DeTramos(Id, _tramos.Select(t => t.TramoId).Append(TramoId)),
        _ => AlcanceEstadistico.DeUnidad(Id, unidadId)
    };

    private JerarquiaEnum ObtenerJerarquia()
        => Nivel?.Jerarquia ?? throw new InvalidOperationException("El nivel de la denominación no fue cargado.");

    public static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("El nombre de la denominación es requerido.");

        var normalizado = nombre.Trim();
        if (normalizado.Length > NombreMaxLength)
            throw new DomainException($"El nombre de la denominación no puede exceder {NombreMaxLength} caracteres.");

        return normalizado;
    }
}
