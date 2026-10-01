using Domain.Abstraction;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Cómo se cerró el evento (TipoCierreAsistenciaEnum en SiGAV 1.0). Es catálogo y no enum para
/// poder agregar o desactivar tipos sin publicar una nueva versión de la app.
/// </summary>
public class TipoCierre : NamedMetadata
{
}
