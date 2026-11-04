using Domain.Entities.Calidad;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Catalogos;

/// <summary>
/// Versión 1 de la encuesta de calidad, publicada: las preguntas de SiGAV 1.0. Los cambios
/// posteriores se hacen desde el front desk como versiones nuevas.
/// </summary>
internal sealed class CalidadSeeder(SiGAVContext context, TimeProvider timeProvider) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Catalogo;
    public int Orden => 60;

    private static readonly IReadOnlyList<DefinicionOpcion> Valoracion =
    [
        new(1, "Muy malo"),
        new(2, "Malo"),
        new(3, "Regular"),
        new(4, "Aceptable"),
        new(5, "Bueno"),
        new(6, "Excelente"),
    ];

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Encuestas.AnyAsync(cancellationToken)) return 0;

        var encuesta = Encuesta.CrearBorrador("Encuesta de satisfacción del servicio", 1,
        [
            new("¿Cómo calificaría la manera de presentarse de nuestros agentes para brindarle el servicio?", TipoPreguntaEnum.Escala, true, true, Valoracion),
            new("¿Cómo describiría nuestro servicio?", TipoPreguntaEnum.Escala, true, true, Valoracion),
            new("¿Le comunicaron que el servicio es gratuito?", TipoPreguntaEnum.SiNo, true, false),
            new("¿Nuestro servicio superó sus expectativas?", TipoPreguntaEnum.SiNo, true, false),
            new("¿Sabía de la existencia de nuestro servicio?", TipoPreguntaEnum.SiNo, true, false),
            new("¿Qué recomienda para mejorar el servicio brindado?", TipoPreguntaEnum.Texto, false, false),
        ]);
        encuesta.Publicar(null, timeProvider.GetUtcNow().UtcDateTime);

        context.Encuestas.Add(encuesta);
        return await context.SaveChangesAsync(cancellationToken);
    }
}
