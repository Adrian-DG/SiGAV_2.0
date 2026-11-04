using Domain.Entities.Calidad;

namespace Domain.Repositories;

public interface IEncuestaRepository
{
    /// <summary>Versión con sus preguntas y opciones.</summary>
    Task<Encuesta?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>La versión publicada (vigente), con sus preguntas y opciones.</summary>
    Task<Encuesta?> GetVigenteAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteBorradorAsync(CancellationToken cancellationToken = default);

    /// <summary>Número de la última versión creada (0 si no hay ninguna).</summary>
    Task<int> GetUltimaVersionAsync(CancellationToken cancellationToken = default);

    void Add(Encuesta encuesta);

    /// <summary>Solo para descartar un borrador.</summary>
    void Remove(Encuesta encuesta);
}
