namespace Domain.Entities.Calidad;

/// <summary>
/// Respuesta a una pregunta de la encuesta. <see cref="Valor"/> es el valor de la opción elegida
/// (escala u opción única) o 1/0 (Sí/No); <see cref="Texto"/> solo en preguntas abiertas.
/// La valida <see cref="PreguntaEncuesta"/> a través de <see cref="EvaluacionCalidad.Responder"/>.
/// </summary>
public class RespuestaEvaluacion
{
    public const int TextoMaxLength = 1000;
    public const int ComentarioMaxLength = 500;

    public int Id { get; private set; }
    public int EvaluacionCalidadId { get; private set; }

    public int PreguntaEncuestaId { get; private set; }
    public virtual PreguntaEncuesta? Pregunta { get; private set; }

    public int? Valor { get; private set; }
    public string? Texto { get; private set; }
    public string? Comentario { get; private set; }

    // Requerido por EF Core
    private RespuestaEvaluacion() { }

    internal static RespuestaEvaluacion Crear(int preguntaId, int? valor, string? texto, string? comentario)
        => new() { PreguntaEncuestaId = preguntaId, Valor = valor, Texto = texto, Comentario = comentario };

    internal void Cambiar(int? valor, string? texto, string? comentario)
    {
        Valor = valor;
        Texto = texto;
        Comentario = comentario;
    }
}
