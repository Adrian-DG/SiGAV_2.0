namespace Domain.Enums;

/// <summary>
/// Categoría de un tipo de evento. Sin valor "NONE": un tipo sin categoría quedaría fuera de
/// las estadísticas por categoría.
/// </summary>
public enum CategoriaEventoEnum
{
    ASISTENCIA = 1,
    ACCIDENTE = 2
}
