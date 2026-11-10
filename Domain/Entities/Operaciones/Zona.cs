using Domain.Abstraction;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Zona de asistencia (antes "región de asistencia"): agrupa tramos y pertenece a una región
/// (<see cref="RegionMacroEnum"/>: Este, Norte o Sur).
/// </summary>
public class Zona : NamedMetadata
{
    public const int NombreMaxLength = 100;

    public RegionMacroEnum Region { get; set; }

    public static Zona Crear(string nombre, RegionMacroEnum region)
    {
        if (!Enum.IsDefined(region)) throw new DomainException("La región de la zona no es válida.");

        return new Zona
        {
            Nombre = NormalizarNombre(nombre, "de la zona", NombreMaxLength),
            Region = region,
            IsActive = true
        };
    }

    /// <param name="de">"de la zona", "del tramo": completa los mensajes.</param>
    internal static string NormalizarNombre(string nombre, string de, int maximo)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException($"El nombre {de} es requerido.");

        var normalizado = nombre.Trim();
        if (normalizado.Length > maximo)
            throw new DomainException($"El nombre {de} no puede exceder {maximo} caracteres.");

        return normalizado;
    }
}
