namespace Domain.Enums;

/// <summary>Por dónde se contactó al ciudadano para la encuesta de calidad.</summary>
public enum CanalContactoEnum
{
    Llamada = 1,

    /// <summary>Reservado para el bot de WhatsApp (aún no implementado).</summary>
    WhatsApp = 2
}
