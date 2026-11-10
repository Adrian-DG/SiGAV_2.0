using Domain.Abstraction;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>Tramo carretero de una <see cref="Zona"/>; se asigna a las denominaciones.</summary>
public class Tramo : NamedMetadata
{
    public const int NombreMaxLength = 100;

    public int ZonaId { get; set; }
    public Zona? Zona { get; set; }

    /// <summary>La zona debe estar activa; lo comprueba quien la busca.</summary>
    public static Tramo Crear(string nombre, Zona zona)
    {
        if (zona.Id <= 0 || !zona.IsActive) throw new DomainException("El tramo debe pertenecer a una zona activa.");

        return new Tramo
        {
            Nombre = Zona.NormalizarNombre(nombre, "del tramo", NombreMaxLength),
            ZonaId = zona.Id,
            IsActive = true
        };
    }
}
