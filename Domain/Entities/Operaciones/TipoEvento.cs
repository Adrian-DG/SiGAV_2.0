using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

[Table("tipo_eventos", Schema = "operaciones")]
public class TipoEvento : NamedMetadata
{
    public CategoriaEventoEnum Categoria { get; set; }
}