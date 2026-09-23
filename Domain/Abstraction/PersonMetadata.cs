using Domain.Enums;

namespace Domain.Abstraction;

public class PersonMetadata : BaseEntityMetadata
{
    public const int IdentificacionMinLength = 8;
    public const int IdentificacionMaxLength = 11;
    public const int NombreMaxLength = 50;
    public const int ApellidoMaxLength = 50;

    public required string Identificacion { get; set; }

    public required string Nombre { get; set; }

    public required string Apellido { get; set; }

    public SexoEnum Sexo { get; set; }
}
