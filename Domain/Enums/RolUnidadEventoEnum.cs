namespace Domain.Enums;

/// <summary>Participación de una unidad en el evento (reemplaza EsPrincipal y UnidadAlfa de SiGAV 1.0).</summary>
public enum RolUnidadEventoEnum
{
    /// <summary>La unidad responsable del evento; siempre hay exactamente una.</summary>
    Principal = 1,

    /// <summary>Unidad que asistió en apoyo.</summary>
    Apoyo = 2,

    /// <summary>Apoyo solicitado (p. ej. la unidad alfa/ambulancia) que aún no llega.</summary>
    ApoyoSolicitado = 3
}
