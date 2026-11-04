namespace Domain.Enums;

/// <summary>
/// Tipos de pregunta de la encuesta de calidad. Todos se pueden responder con un número o una
/// palabra, para que sirvan igual en una llamada que en un bot de mensajería.
/// </summary>
public enum TipoPreguntaEnum
{
    /// <summary>Opciones ordenadas con valor numérico (p. ej. 1 = Muy malo … 5 = Excelente): se pueden promediar.</summary>
    Escala = 1,

    /// <summary>Sí (1) o No (0).</summary>
    SiNo = 2,

    /// <summary>Una opción de una lista, sin orden (no se promedia).</summary>
    OpcionUnica = 3,

    /// <summary>Respuesta libre.</summary>
    Texto = 4
}
