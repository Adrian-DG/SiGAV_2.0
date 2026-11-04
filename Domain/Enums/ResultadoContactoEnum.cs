namespace Domain.Enums;

/// <summary>Resultado de un intento de contacto con el ciudadano.</summary>
public enum ResultadoContactoEnum
{
    /// <summary>El ciudadano atendió: se puede tomar la encuesta.</summary>
    Contesto = 1,
    NoContesto = 2,
    NumeroEquivocado = 3,
    Buzon = 4,

    /// <summary>Atendió pero no quiso responder: cierra la evaluación como rechazada.</summary>
    RechazoEncuesta = 5,

    /// <summary>Canal de mensajería: se envió el mensaje inicial.</summary>
    MensajeEnviado = 6,

    /// <summary>Canal de mensajería: no respondió dentro del plazo.</summary>
    SinRespuesta = 7
}
