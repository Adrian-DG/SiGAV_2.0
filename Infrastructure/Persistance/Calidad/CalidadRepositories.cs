using Domain.Entities.Calidad;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Calidad;

public class EncuestaRepository(SiGAVContext context) : IEncuestaRepository
{
    public Task<Encuesta?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => ConPreguntas().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Encuesta?> GetVigenteAsync(CancellationToken cancellationToken = default)
        => ConPreguntas().FirstOrDefaultAsync(e => e.Estado == EstadoEncuestaEnum.Publicada, cancellationToken);

    public Task<bool> ExisteBorradorAsync(CancellationToken cancellationToken = default)
        => context.Encuestas.AnyAsync(e => e.Estado == EstadoEncuestaEnum.Borrador, cancellationToken);

    public async Task<int> GetUltimaVersionAsync(CancellationToken cancellationToken = default)
        => await context.Encuestas.MaxAsync(e => (int?)e.Version, cancellationToken) ?? 0;

    public void Add(Encuesta encuesta) => context.Encuestas.Add(encuesta);

    public void Remove(Encuesta encuesta) => context.Encuestas.Remove(encuesta);

    private IQueryable<Encuesta> ConPreguntas()
        => context.Encuestas.Include(e => e.Preguntas).ThenInclude(p => p.Opciones).AsSplitQuery();
}

public class EvaluacionCalidadRepository(SiGAVContext context) : IEvaluacionCalidadRepository
{
    public Task<EvaluacionCalidad?> GetByEventoIdAsync(int eventoId, CancellationToken cancellationToken = default)
        => context.EvaluacionesCalidad
            .Include(e => e.Intentos)
            .Include(e => e.Respuestas)
            .Include(e => e.Encuesta!).ThenInclude(en => en.Preguntas).ThenInclude(p => p.Opciones)
            .Include(e => e.Evento!).ThenInclude(ev => ev.Ciudadanos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.EventoId == eventoId, cancellationToken);

    public Task<bool> ExisteParaEventoAsync(int eventoId, CancellationToken cancellationToken = default)
        => context.EvaluacionesCalidad.AnyAsync(e => e.EventoId == eventoId, cancellationToken);

    public void Add(EvaluacionCalidad evaluacion) => context.EvaluacionesCalidad.Add(evaluacion);
}
