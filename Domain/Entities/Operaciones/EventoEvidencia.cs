using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Foto o firma asociada al evento. Solo se guarda la referencia: el archivo vive en el
/// almacenamiento de objetos (no en la base de datos, a diferencia de SiGAV 1.0).
/// </summary>
public class EventoEvidencia
{
    public int EventoId { get; private set; }
    public int EventoCiudadanoId { get; private set; }

    public TipoEvidenciaEnum Tipo { get; private set; }

    /// <summary>Clave o URL del archivo en el almacenamiento.</summary>
    public string ContentType { get; private set; } = null!;

    public DateTime RegistradaUtc { get; private set; }

    // Requerido por EF Core
    private EventoEvidencia() { }

    internal static EventoEvidencia Crear(TipoEvidenciaEnum tipo, string contentType, DateTime registradaUtc)
    {
        if (!Enum.IsDefined(tipo)) throw new DomainException("El tipo de evidencia no es válido.");

        return new EventoEvidencia
        {
            Tipo = tipo,
            ContentType = contentType.Trim().ToLowerInvariant(),
            RegistradaUtc = DateTime.SpecifyKind(registradaUtc, DateTimeKind.Utc)
        };
    }
}
