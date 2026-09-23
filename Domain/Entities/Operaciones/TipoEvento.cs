using Domain.Abstraction;
using Domain.Enums;

namespace Domain.Entities.Operaciones;

public class TipoEvento : NamedMetadata
{
    public CategoriaEventoEnum Categoria { get; set; }
}