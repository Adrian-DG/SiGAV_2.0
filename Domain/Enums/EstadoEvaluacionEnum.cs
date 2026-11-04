namespace Domain.Enums;

/// <summary>Estado de la evaluación de calidad de un evento. "Pendiente" es la ausencia de evaluación.</summary>
public enum EstadoEvaluacionEnum
{
    /// <summary>Se está intentando contactar al ciudadano o tomando la encuesta.</summary>
    EnGestion = 1,

    Completada = 2,

    /// <summary>Se cerró sin poder contactar al ciudadano.</summary>
    NoContactado = 3,

    /// <summary>El ciudadano no quiso responder la encuesta.</summary>
    Rechazada = 4
}
