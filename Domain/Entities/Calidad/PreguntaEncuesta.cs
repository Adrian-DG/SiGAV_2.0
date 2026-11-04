using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Calidad;

/// <summary>Pregunta de una versión de la encuesta. Solo se crea o cambia a través de <see cref="Encuesta"/>.</summary>
public class PreguntaEncuesta
{
    public const int TextoMaxLength = 300;
    public const int MaxOpciones = 10;
    public const int EtiquetaMaxLength = 60;

    /// <summary>Valores de una pregunta Sí/No.</summary>
    public const int ValorSi = 1;
    public const int ValorNo = 0;

    public int Id { get; private set; }
    public int EncuestaId { get; private set; }

    /// <summary>Posición en la encuesta, desde 1.</summary>
    public int Orden { get; private set; }
    public string Texto { get; private set; } = null!;
    public TipoPreguntaEnum Tipo { get; private set; }
    public bool Obligatoria { get; private set; }

    /// <summary>Admite un comentario además de la respuesta (p. ej. por qué calificó así).</summary>
    public bool PermiteComentario { get; private set; }

    private readonly List<OpcionPregunta> _opciones = new();

    /// <summary>Solo en Escala y OpcionUnica, por valor.</summary>
    public IReadOnlyCollection<OpcionPregunta> Opciones => _opciones.OrderBy(o => o.Valor).ToList().AsReadOnly();

    // Requerido por EF Core
    private PreguntaEncuesta() { }

    public static bool UsaOpciones(TipoPreguntaEnum tipo) => tipo is TipoPreguntaEnum.Escala or TipoPreguntaEnum.OpcionUnica;

    internal static PreguntaEncuesta Crear(int orden, DefinicionPregunta definicion)
    {
        ArgumentNullException.ThrowIfNull(definicion);
        if (!Enum.IsDefined(definicion.Tipo)) throw new DomainException($"El tipo de la pregunta {orden} no es válido.");

        var texto = definicion.Texto?.Trim();
        if (string.IsNullOrEmpty(texto)) throw new DomainException($"Indique el texto de la pregunta {orden}.");
        if (texto.Length > TextoMaxLength) throw new DomainException($"La pregunta {orden} no puede exceder {TextoMaxLength} caracteres.");

        var pregunta = new PreguntaEncuesta
        {
            Orden = orden,
            Texto = texto,
            Tipo = definicion.Tipo,
            Obligatoria = definicion.Obligatoria,
            PermiteComentario = definicion.PermiteComentario
        };

        var opciones = definicion.Opciones ?? [];
        if (UsaOpciones(definicion.Tipo))
        {
            if (opciones.Count < 2) throw new DomainException($"La pregunta {orden} debe tener al menos 2 opciones.");
            if (opciones.Count > MaxOpciones) throw new DomainException($"La pregunta {orden} no puede tener más de {MaxOpciones} opciones.");
            if (opciones.Select(o => o.Valor).Distinct().Count() != opciones.Count)
                throw new DomainException($"Las opciones de la pregunta {orden} no pueden repetir el valor.");

            foreach (var opcion in opciones)
                pregunta._opciones.Add(OpcionPregunta.Crear(opcion, orden));

            if (pregunta._opciones.Select(o => o.Etiqueta.ToUpperInvariant()).Distinct().Count() != opciones.Count)
                throw new DomainException($"Las opciones de la pregunta {orden} no pueden repetir la etiqueta.");
        }
        else if (opciones.Count > 0)
        {
            throw new DomainException($"La pregunta {orden} no admite opciones (solo las de escala u opción única).");
        }

        return pregunta;
    }

    internal DefinicionPregunta ADefinicion()
        => new(Texto, Tipo, Obligatoria, PermiteComentario, Opciones.Select(o => new DefinicionOpcion(o.Valor, o.Etiqueta)).ToList());

    /// <summary>
    /// Valida una respuesta y la devuelve normalizada. Sin valor ni texto, la pregunta queda sin
    /// responder (devuelve null).
    /// </summary>
    internal (int? Valor, string? Texto, string? Comentario)? Normalizar(int? valor, string? texto, string? comentario)
    {
        texto = Limpiar(texto, RespuestaEvaluacion.TextoMaxLength, "La respuesta");
        comentario = Limpiar(comentario, RespuestaEvaluacion.ComentarioMaxLength, "El comentario");

        if (comentario is not null && !PermiteComentario)
            throw new DomainException($"La pregunta {Orden} no admite comentario.");

        if (Tipo == TipoPreguntaEnum.Texto)
        {
            if (valor is not null) throw new DomainException($"La pregunta {Orden} se responde con texto.");
            return texto is null && comentario is null ? null : (null, texto, comentario);
        }

        if (texto is not null) throw new DomainException($"La pregunta {Orden} se responde eligiendo una opción.");
        if (valor is null) return comentario is null ? null : throw new DomainException($"Responda la pregunta {Orden} para agregar un comentario.");

        var valido = Tipo == TipoPreguntaEnum.SiNo
            ? valor is ValorSi or ValorNo
            : _opciones.Any(o => o.Valor == valor);
        if (!valido) throw new DomainException($"La respuesta de la pregunta {Orden} no es una de sus opciones.");

        return (valor, null, comentario);
    }

    private static string? Limpiar(string? valor, int maxLength, string campo)
    {
        var limpio = valor?.Trim();
        if (string.IsNullOrEmpty(limpio)) return null;
        if (limpio.Length > maxLength) throw new DomainException($"{campo} no puede exceder {maxLength} caracteres.");
        return limpio;
    }
}

/// <summary>Opción de una pregunta de escala u opción única.</summary>
public class OpcionPregunta
{
    public int Id { get; private set; }
    public int PreguntaEncuestaId { get; private set; }

    /// <summary>Lo que se guarda como respuesta. En una escala, mayor = mejor.</summary>
    public int Valor { get; private set; }
    public string Etiqueta { get; private set; } = null!;

    // Requerido por EF Core
    private OpcionPregunta() { }

    internal static OpcionPregunta Crear(DefinicionOpcion definicion, int ordenPregunta)
    {
        var etiqueta = definicion.Etiqueta?.Trim();
        if (string.IsNullOrEmpty(etiqueta)) throw new DomainException($"Las opciones de la pregunta {ordenPregunta} deben tener etiqueta.");
        if (etiqueta.Length > PreguntaEncuesta.EtiquetaMaxLength)
            throw new DomainException($"Las opciones de la pregunta {ordenPregunta} no pueden exceder {PreguntaEncuesta.EtiquetaMaxLength} caracteres.");
        return new OpcionPregunta { Valor = definicion.Valor, Etiqueta = etiqueta };
    }
}
