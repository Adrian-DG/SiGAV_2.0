using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Catálogo de niveles de denominación (antes TipoUnidad): Encargado Regional, Encargado de
/// Tramo, Móvil, Motorizada, Ambulancia... Pertenece a la denominación, no a la ficha.
/// </summary>
[Table("niveles_denominacion", Schema = "operaciones")]
public class NivelDenominacion : NamedMetadata
{
    public JerarquiaEnum Jerarquia { get; set; } = JerarquiaEnum.Unidad;

    /// <summary>
    /// Reemplaza la lista fija de Ids de ambulancia de SiGAV 1.0 (6, 14 y 15).
    /// </summary>
    public bool EsAmbulancia { get; set; }

    public bool EsEncargado => Jerarquia != JerarquiaEnum.Unidad;
}
