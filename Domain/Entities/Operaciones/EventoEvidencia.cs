using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Foto o firma asociada al evento. Solo se guarda la referencia: el archivo vive en el
/// almacenamiento de objetos (no en la base de datos, a diferencia de SiGAV 1.0).
/// </summary>
public class EventoEvidencia
{
    public const int UbicacionMaxLength = 500;
    public const int ContentTypeMaxLength = 100;

    public int Id { get; private set; }
    public int EventoId { get; private set; }

    public TipoEvidenciaEnum Tipo { get; private set; }

    /// <summary>Clave o URL del archivo en el almacenamiento.</summary>
    public string Ubicacion { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public DateTime RegistradaUtc { get; private set; }

    // Requerido por EF Core
    private EventoEvidencia() { }

    internal static EventoEvidencia Crear(TipoEvidenciaEnum tipo, string ubicacion, string contentType, DateTime registradaUtc)
    {
        if (!Enum.IsDefined(tipo)) throw new DomainException("El tipo de evidencia no es válido.");
        if (string.IsNullOrWhiteSpace(ubicacion) || ubicacion.Length > UbicacionMaxLength)
            throw new DomainException($"La ubicación de la evidencia es requerida y no puede exceder {UbicacionMaxLength} caracteres.");
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > ContentTypeMaxLength
            || !(contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("La evidencia debe ser una imagen.");

        return new EventoEvidencia
        {
            Tipo = tipo,
            Ubicacion = ubicacion.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            RegistradaUtc = DateTime.SpecifyKind(registradaUtc, DateTimeKind.Utc)
        };
    }
}
