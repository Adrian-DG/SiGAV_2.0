using Domain.Entities.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations.Operaciones;

internal sealed class AgenteConfiguration : IEntityTypeConfiguration<Agente>
{
    public void Configure(EntityTypeBuilder<Agente> builder)
    {
        builder.ToTable("agentes", Schemas.Operaciones);
        builder.ConfigurePerson();

        builder.HasIndex(a => a.Identificacion).IsUnique();
        builder.Property(a => a.Especialidad).HasMaxLength(Agente.EspecialidadMaxLength);
        builder.HasIndex(a => new { a.AreaOperativa, a.IsActive });

        builder.HasOne(a => a.Rango)
            .WithMany()
            .HasForeignKey(a => a.RangoId);
    }
}
