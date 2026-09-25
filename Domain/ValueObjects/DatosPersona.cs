using Domain.Enums;
using Domain.Exceptions;

namespace Domain.ValueObjects;

/// <summary>
/// Datos de una persona tal como se registraron en el evento. Se guardan en el propio evento
/// para que corregir el maestro de ciudadanos no altere reportes históricos. Admite personas
/// desconocidas o indocumentadas (sin identificación ni nombre).
/// </summary>
public sealed record DatosPersona
{
    public const int IdentificacionMaxLength = 20; // cédula o pasaporte
    public const int NombreMaxLength = 50;
    public const int TelefonoMaxLength = 20;

    public string? Identificacion { get; private init; }
    public string? Nombre { get; private init; }
    public string? Apellido { get; private init; }
    public SexoEnum Sexo { get; private init; }
    public string? Telefono { get; private init; }
    public int? NacionalidadId { get; private init; }

    public bool EsDesconocida => Identificacion is null && Nombre is null && Apellido is null;

    // Requerido por EF Core
    private DatosPersona() { }

    public DatosPersona(string? identificacion, string? nombre, string? apellido, SexoEnum sexo, string? telefono, int? nacionalidadId)
    {
        if (!Enum.IsDefined(sexo)) throw new DomainException("El sexo no es válido.");
        if (nacionalidadId is <= 0) throw new DomainException("La nacionalidad no es válida.");

        Identificacion = NormalizarIdentificacion(identificacion);
        Nombre = Normalizar(nombre, NombreMaxLength, "nombre");
        Apellido = Normalizar(apellido, NombreMaxLength, "apellido");
        Telefono = Normalizar(telefono, TelefonoMaxLength, "teléfono");
        Sexo = sexo;
        NacionalidadId = nacionalidadId;
    }

    public static DatosPersona Desconocida(SexoEnum sexo = SexoEnum.NONE) => new(null, null, null, sexo, null, null);

    /// <summary>Misma normalización que al guardar (sin guiones, mayúsculas): para buscar por cédula/pasaporte.</summary>
    public static string? NormalizarIdentificacion(string? identificacion)
        => Normalizar(identificacion, IdentificacionMaxLength, "identificación", soloAlfanumericos: true);

    internal static string? Normalizar(string? valor, int maxLength, string campo, bool soloAlfanumericos = false)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        var normalizado = soloAlfanumericos
            ? new string(valor.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant()
            : valor.Trim();

        if (normalizado.Length == 0) return null;
        if (normalizado.Length > maxLength)
            throw new DomainException($"El {campo} no puede exceder {maxLength} caracteres.");

        return normalizado;
    }
}
