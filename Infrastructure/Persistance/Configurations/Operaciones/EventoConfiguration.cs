using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("eventos", Schemas.Operaciones);
        builder.ConfigureBaseEntity();

        builder.HasIndex(e => e.CreatedAt);
        builder.HasOne(e => e.Municipio)
            .WithMany()
            .HasForeignKey(e => e.MunicipioId);
    }
}
