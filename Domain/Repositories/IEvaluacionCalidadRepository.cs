using Domain.Entities.Calidad;

namespace Domain.Repositories;

public interface IEvaluacionCalidadRepository
{
    /// <summary>Evaluación con intentos, respuestas, su encuesta (preguntas y opciones) y su evento (con personas).</summary>
    Task<EvaluacionCalidad?> GetByEventoIdAsync(int eventoId, CancellationToken cancellationToken = default);

    Task<bool> ExisteParaEventoAsync(int eventoId, CancellationToken cancellationToken = default);

    void Add(EvaluacionCalidad evaluacion);
}
