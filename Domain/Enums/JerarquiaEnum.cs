namespace Domain.Enums;

/// <summary>
/// Posición de una denominación en la cadena de supervisión. Define qué estadísticas puede ver.
/// </summary>
public enum JerarquiaEnum
{
    /// <summary>Encargado regional: todos los tramos de las regiones (macro o de asistencia) asignadas.</summary>
    Regional = 1,

    /// <summary>Encargado de tramo: su tramo y los tramos que tenga asignados.</summary>
    Tramo = 2,

    /// <summary>Móviles, motorizadas y demás unidades operativas: solo sus propias estadísticas.</summary>
    Unidad = 3
}
