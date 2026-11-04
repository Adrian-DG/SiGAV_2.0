namespace Domain.Enums;

/// <summary>Ciclo de vida de una versión de la encuesta de calidad.</summary>
public enum EstadoEncuestaEnum
{
    /// <summary>En edición. Solo puede haber un borrador a la vez.</summary>
    Borrador = 1,

    /// <summary>Vigente: la usan las evaluaciones nuevas. Solo una a la vez; ya no se modifica.</summary>
    Publicada = 2,

    /// <summary>Reemplazada por una versión posterior. Las evaluaciones que la usaron la conservan.</summary>
    Retirada = 3
}
