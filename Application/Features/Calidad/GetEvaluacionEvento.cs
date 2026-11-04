using Application.Contracts;
using Application.Exceptions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Calidad;

/// <summary>
/// Todo lo necesario para evaluar un evento: su resumen y personas (con teléfono), la evaluación si
/// ya se inició y la encuesta a responder (la de la evaluación o la vigente).
/// </summary>
public record GetEvaluacionEventoQuery(int EventoId) : IRequest<EvaluacionEventoViewModel>;

public class GetEvaluacionEventoQueryHandler(IReadDbContext db) : IRequestHandler<GetEvaluacionEventoQuery, EvaluacionEventoViewModel>
{
    public async Task<EvaluacionEventoViewModel> Handle(GetEvaluacionEventoQuery request, CancellationToken cancellationToken)
    {
        var evento = await db.Eventos
            .Where(e => e.Id == request.EventoId && e.IsActive && e.Estado == EstadoEventoEnum.Completado)
            .Select(e => new EventoCalidadViewModel(
                e.Id,
                e.FechaHoraReporteUtc,
                e.FechaHoraCompletadoUtc,
                e.Tipos.Select(t => t.TipoEvento!.Nombre).Distinct().ToList(),
                e.Direccion,
                e.Municipio != null ? e.Municipio.Nombre : null,
                e.Municipio != null && e.Municipio.Provincia != null ? e.Municipio.Provincia.Nombre : null,
                e.Tramo != null ? e.Tramo.Nombre : null,
                e.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Unidad!.Ficha).FirstOrDefault() ?? "",
                e.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Denominacion!.Nombre).FirstOrDefault() ?? "",
                e.TipoCierre != null ? e.TipoCierre.Nombre : null,
                e.Comentario,
                e.Ciudadanos
                    .OrderBy(c => c.Id)
                    .Select(c => new PersonaEventoCalidadViewModel(
                        c.Id,
                        c.Rol,
                        ((c.Persona.Nombre ?? "") + " " + (c.Persona.Apellido ?? "")).Trim(),
                        c.Persona.Identificacion,
                        c.Persona.Telefono))
                    .ToList()))
            
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("El evento completado", request.EventoId);

        var evaluacion = await db.EvaluacionesCalidad
            .Where(c => c.EventoId == request.EventoId)
            .Select(c => new
            {
                c.Id,
                c.Estado,
                c.Canal,
                c.EncuestaId,
                c.EventoCiudadanoId,
                c.NombreContacto,
                c.TelefonoContacto,
                c.Observacion,
                c.CreatedAt,
                c.CreatedBy,
                c.CerradaUtc,
                c.CerradaPorUsuarioId,
                Intentos = c.Intentos.OrderBy(i => i.FechaUtc).ThenBy(i => i.Id)
                    .Select(i => new { i.FechaUtc, i.Canal, i.Resultado, i.UsuarioId, i.Observacion }).ToList(),
                Respuestas = c.Respuestas
                    .Select(r => new RespuestaViewModel(r.PreguntaEncuestaId, r.Valor, r.Texto, r.Comentario)).ToList()
            })
            
            .FirstOrDefaultAsync(cancellationToken);

        var encuestaQuery = db.Encuestas.Include(e => e.Preguntas).ThenInclude(p => p.Opciones);
        var encuesta = evaluacion is not null
            ? await encuestaQuery.FirstOrDefaultAsync(e => e.Id == evaluacion.EncuestaId, cancellationToken)
            : await encuestaQuery.FirstOrDefaultAsync(e => e.Estado == EstadoEncuestaEnum.Publicada, cancellationToken);

        EvaluacionViewModel? vm = null;
        if (evaluacion is not null)
        {
            var usuarioIds = evaluacion.Intentos.Select(i => i.UsuarioId)
                .Append(evaluacion.CerradaPorUsuarioId)
                .Append(evaluacion.CreatedBy)
                .OfType<int>()
                .Distinct()
                .ToList();
            var usuarios = await db.Usuarios
                .Where(u => usuarioIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName, cancellationToken);
            string? Nombre(int? id) => id is { } i && usuarios.TryGetValue(i, out var n) ? n : null;

            vm = new EvaluacionViewModel(
                evaluacion.Id,
                evaluacion.Estado,
                evaluacion.Canal,
                evaluacion.EventoCiudadanoId,
                evaluacion.NombreContacto,
                evaluacion.TelefonoContacto,
                evaluacion.Observacion,
                evaluacion.CreatedAt,
                Nombre(evaluacion.CreatedBy),
                evaluacion.CerradaUtc,
                evaluacion.CerradaUtc is null ? null : Nombre(evaluacion.CerradaPorUsuarioId) ?? (evaluacion.CerradaPorUsuarioId is null ? "Bot" : null),
                evaluacion.Intentos
                    .Select(i => new IntentoContactoViewModel(i.FechaUtc, i.Canal, i.Resultado, i.UsuarioId is null ? "Bot" : Nombre(i.UsuarioId), i.Observacion))
                    .ToList(),
                evaluacion.Respuestas);
        }

        return new EvaluacionEventoViewModel(evento, vm, encuesta is null ? null : EncuestaViewModel.Desde(encuesta));
    }
}
