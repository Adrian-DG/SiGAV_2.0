using Domain.Abstraction;

namespace Domain.Entities.Misc;

/// <summary>
/// Prefijo de placa asignado por la DGII (A automóvil privado, G jeep, K motocicleta...) con el
/// formato completo de la placa y los tipos de vehículo que le corresponden. Es catálogo para poder
/// corregir formatos o agregar prefijos sin publicar otra versión de la app.
/// </summary>
public class PrefijoPlaca : NamedMetadata
{
    public const int PrefijoMaxLength = 5;
    public const int PatronMaxLength = 100;
    public const int EjemploMaxLength = 10;

    /// <summary>Letras iniciales de la placa, en mayúsculas.</summary>
    public required string Prefijo { get; set; }

    /// <summary>
    /// Expresión regular de la placa completa ya normalizada (sin guiones ni espacios, en
    /// mayúsculas). Debe ser compatible con .NET y con JavaScript: anclas, clases y cuantificadores
    /// simples, p. ej. <c>^A\d{6}$</c>.
    /// </summary>
    public required string Patron { get; set; }

    /// <summary>Placa de ejemplo para los mensajes de validación.</summary>
    public required string Ejemplo { get; set; }

    /// <summary>Tipos de vehículo que corresponden al prefijo. Vacío = cualquiera (p. ej. exhibición).</summary>
    public virtual ICollection<TipoVehiculo> TiposVehiculo { get; set; } = new List<TipoVehiculo>();
}
