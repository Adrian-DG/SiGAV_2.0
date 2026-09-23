using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

[Table("denominaciones", Schema = "operaciones")]
public class Denominacion : NamedMetadata, IAuditableMetadata
{
    public const int NombreMaxLength = 100;

    [ForeignKey(nameof(Tramo))]
    public int TramoId { get; private set; }
    public virtual Tramo? Tramo { get; private set; }

    [ForeignKey(nameof(TipoUnidad))]
    public int TipoUnidadId { get; private set; }
    public virtual TipoUnidad? TipoUnidad { get; private set; }

    public DateOnly CreatedAt { get; set; }
    public DateOnly? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private Denominacion() { }

    public static Denominacion Crear(string nombre, int tramoId, int tipoUnidadId)
    {
        var nombreNormalizado = NormalizarNombre(nombre);

        if (tramoId <= 0) throw new DomainException("La denominación debe pertenecer a un tramo válido.");
        if (tipoUnidadId <= 0) throw new DomainException("La denominación debe tener un tipo de unidad válido.");

        return new Denominacion
        {
            Nombre = nombreNormalizado,
            TramoId = tramoId,
            TipoUnidadId = tipoUnidadId,
            IsActive = true
        };
    }

    public void CambiarTipoUnidad(int tipoUnidadId)
    {
        if (tipoUnidadId <= 0) throw new DomainException("El tipo de unidad no es válido.");
        TipoUnidadId = tipoUnidadId;
    }

    public static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("El nombre de la denominación es requerido.");

        var normalizado = nombre.Trim();
        if (normalizado.Length > NombreMaxLength)
            throw new DomainException($"El nombre de la denominación no puede exceder {NombreMaxLength} caracteres.");

        return normalizado;
    }
}
