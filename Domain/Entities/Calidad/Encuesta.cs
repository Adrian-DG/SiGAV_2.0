using Domain.Abstraction;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Calidad;

/// <summary>
/// Una versión de la encuesta de calidad que se hace al ciudadano después de un evento. Las
/// preguntas cambian con el tiempo: cada cambio es una versión nueva (copia de la vigente), y una
/// versión publicada no se modifica nunca, así que toda evaluación conserva las preguntas con que
/// se respondió. Solo hay una versión publicada (vigente) y, como mucho, un borrador.
/// </summary>
public class Encuesta : BaseEntityMetadata, IAuditableMetadata
{
    public const int NombreMaxLength = 100;
    public const int MaxPreguntas = 30;

    public string Nombre { get; private set; } = null!;
    public int Version { get; private set; }
    public EstadoEncuestaEnum Estado { get; private set; }
    public DateTime? PublicadaUtc { get; private set; }
    public DateTime? RetiradaUtc { get; private set; }

    private readonly List<PreguntaEncuesta> _preguntas = new();

    /// <summary>En el orden en que se hacen.</summary>
    public IReadOnlyCollection<PreguntaEncuesta> Preguntas => _preguntas.OrderBy(p => p.Orden).ToList().AsReadOnly();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private Encuesta() { }

    /// <summary>Borrador nuevo. <paramref name="version"/> debe ser la siguiente a la última existente.</summary>
    public static Encuesta CrearBorrador(string nombre, int version, IReadOnlyList<DefinicionPregunta> preguntas)
    {
        if (version < 1) throw new DomainException("La versión de la encuesta debe ser mayor que cero.");

        var encuesta = new Encuesta { Version = version, Estado = EstadoEncuestaEnum.Borrador, IsActive = true };
        encuesta.Definir(nombre, preguntas);
        return encuesta;
    }

    /// <summary>Borrador de la siguiente versión, con las mismas preguntas de <paramref name="origen"/>.</summary>
    public static Encuesta NuevaVersionDe(Encuesta origen, int version)
    {
        ArgumentNullException.ThrowIfNull(origen);
        if (version <= origen.Version)
            throw new DomainException("La nueva versión debe ser posterior a la versión de origen.");

        return CrearBorrador(origen.Nombre, version, origen.Preguntas.Select(p => p.ADefinicion()).ToList());
    }

    /// <summary>Reemplaza nombre y preguntas. Solo en borrador.</summary>
    public void ActualizarBorrador(string nombre, IReadOnlyList<DefinicionPregunta> preguntas)
    {
        if (Estado != EstadoEncuestaEnum.Borrador)
            throw new DomainException("Solo se puede modificar una encuesta en borrador; para cambiar la vigente cree una nueva versión.");
        Definir(nombre, preguntas);
    }

    /// <summary>
    /// La convierte en la versión vigente y retira la anterior (<paramref name="vigente"/>), de modo
    /// que las evaluaciones nuevas usen esta.
    /// </summary>
    public void Publicar(Encuesta? vigente, DateTime ahoraUtc)
    {
        if (Estado != EstadoEncuestaEnum.Borrador) throw new DomainException("Solo se puede publicar una encuesta en borrador.");
        if (_preguntas.Count == 0) throw new DomainException("La encuesta debe tener al menos una pregunta.");
        if (vigente is not null)
        {
            if (vigente.Estado != EstadoEncuestaEnum.Publicada) throw new DomainException("La encuesta a reemplazar no es la vigente.");
            vigente.Estado = EstadoEncuestaEnum.Retirada;
            vigente.RetiradaUtc = ahoraUtc;
        }

        Estado = EstadoEncuestaEnum.Publicada;
        PublicadaUtc = ahoraUtc;
    }

    public PreguntaEncuesta Pregunta(int preguntaId)
        => _preguntas.FirstOrDefault(p => p.Id == preguntaId)
           ?? throw new DomainException($"La pregunta {preguntaId} no pertenece a la versión {Version} de la encuesta.");

    private void Definir(string nombre, IReadOnlyList<DefinicionPregunta> preguntas)
    {
        ArgumentNullException.ThrowIfNull(preguntas);
        var limpio = nombre?.Trim();
        if (string.IsNullOrEmpty(limpio)) throw new DomainException("Indique el nombre de la encuesta.");
        if (limpio.Length > NombreMaxLength) throw new DomainException($"El nombre de la encuesta no puede exceder {NombreMaxLength} caracteres.");
        if (preguntas.Count > MaxPreguntas) throw new DomainException($"La encuesta no puede tener más de {MaxPreguntas} preguntas.");

        // Validar todas antes de tocar las actuales
        var nuevas = preguntas.Select((p, i) => PreguntaEncuesta.Crear(i + 1, p)).ToList();

        Nombre = limpio;
        _preguntas.Clear();
        _preguntas.AddRange(nuevas);
    }
}

/// <summary>Datos de una pregunta para crear o editar un borrador. El orden es la posición en la lista.</summary>
public sealed record DefinicionPregunta(
    string Texto,
    TipoPreguntaEnum Tipo,
    bool Obligatoria,
    bool PermiteComentario,
    IReadOnlyList<DefinicionOpcion>? Opciones = null);

public sealed record DefinicionOpcion(int Valor, string Etiqueta);
