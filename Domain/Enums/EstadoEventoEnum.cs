namespace Domain.Enums;

/// <summary>Ciclo de vida del evento (EstatusAsistencia en SiGAV 1.0, mismos valores).</summary>
public enum EstadoEventoEnum
{
    /// <summary>Registrado (p. ej. por el centro de operaciones) y aún no atendido.</summary>
    Pendiente = 1,

    /// <summary>La unidad llegó y lo está atendiendo.</summary>
    EnCurso = 2,

    Completado = 3
}
