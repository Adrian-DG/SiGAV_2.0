using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Foto o firma asociada al evento. Solo se guarda la referencia: el archivo vive en el
/// almacenamiento de objetos (no en la base de datos, a diferencia de SiGAV 1.0).
/// Opcionalmente indica a qué persona (cédula, firma) o vehículo (placa) del evento corresponde.
/// </summary>
public class EventoEvidencia
{
    public const int UbicacionMaxLength = 500;
    public const int ContentTypeMaxLength = 100;

    public int Id { get; private set; }
    public int EventoId { get; private set; }

    /// <summary>
    /// Clave de idempotencia enviada por la app: un reenvío de la cola offline con el mismo
    /// RequestId no sube la evidencia dos veces.
    /// </summary>
    public Guid? RequestId { get; private set; }

    public TipoEvidenciaEnum Tipo { get; private set; }

    /// <summary>Clave del archivo en el almacenamiento (no una URL: el proveedor puede cambiar).</summary>
    public string Ubicacion { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long TamanoBytes { get; private set; }

    /// <summary>Persona del evento a la que corresponde (cédula, firma del ciudadano...).</summary>
    public int? EventoCiudadanoId { get; private set; }
    public virtual EventoCiudadanoInfo? Ciudadano { get; private set; }

    /// <summary>Vehículo del evento al que corresponde (foto de la placa...).</summary>
    public int? EventoVehiculoId { get; private set; }
    public virtual EventoVehiculoInfo? Vehiculo { get; private set; }

    public DateTime RegistradaUtc { get; private set; }

    // Requerido por EF Core
    private EventoEvidencia() { }

    /// <summary>Si el tipo puede asociarse a una persona del evento.</summary>
    public static bool AdmiteCiudadano(TipoEvidenciaEnum tipo)
        => tipo is TipoEvidenciaEnum.FotoCedula or TipoEvidenciaEnum.FirmaCiudadano or TipoEvidenciaEnum.Foto;

    /// <summary>Si el tipo puede asociarse a un vehículo del evento.</summary>
    public static bool AdmiteVehiculo(TipoEvidenciaEnum tipo)
        => tipo is TipoEvidenciaEnum.FotoPlaca or TipoEvidenciaEnum.Foto;

    internal static EventoEvidencia Crear(
        TipoEvidenciaEnum tipo,
        string ubicacion,
        string contentType,
        long tamanoBytes,
        DateTime registradaUtc,
        Guid? requestId,
        EventoCiudadanoInfo? ciudadano,
        EventoVehiculoInfo? vehiculo)
    {
        if (!Enum.IsDefined(tipo)) throw new DomainException("El tipo de evidencia no es válido.");
        if (string.IsNullOrWhiteSpace(ubicacion) || ubicacion.Length > UbicacionMaxLength)
            throw new DomainException($"La ubicación de la evidencia es requerida y no puede exceder {UbicacionMaxLength} caracteres.");
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > ContentTypeMaxLength
            || !(contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("La evidencia debe ser una imagen.");
        if (tamanoBytes <= 0) throw new DomainException("El archivo de la evidencia está vacío.");
        if (ciudadano is not null && !AdmiteCiudadano(tipo))
            throw new DomainException("Este tipo de evidencia no puede asociarse a una persona.");
        if (vehiculo is not null && !AdmiteVehiculo(tipo))
            throw new DomainException("Este tipo de evidencia no puede asociarse a un vehículo.");
        if (ciudadano is not null && vehiculo is not null)
            throw new DomainException("La evidencia se asocia a una persona o a un vehículo, no a ambos.");

        return new EventoEvidencia
        {
            Tipo = tipo,
            Ubicacion = ubicacion.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            TamanoBytes = tamanoBytes,
            RegistradaUtc = DateTime.SpecifyKind(registradaUtc, DateTimeKind.Utc),
            RequestId = requestId,
            Ciudadano = ciudadano,
            Vehiculo = vehiculo
        };
    }
}
