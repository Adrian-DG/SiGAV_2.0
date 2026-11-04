using Application.Common;
using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Calidad;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Calidad;

/// <summary>
/// Excel con las evaluaciones de los eventos reportados en el rango (días operativos de RD) que usaron
/// una versión de la encuesta (por defecto la vigente): una columna por pregunta de esa versión, más
/// su comentario si lo admite. Las preguntas cambian entre versiones, por eso se exporta una a la vez.
/// </summary>
public record ExportarEvaluacionesQuery(DateOnly Desde, DateOnly Hasta, int? EncuestaId = null) : IRequest<ArchivoExportado>;

public record ArchivoExportado(byte[] Contenido, string Nombre);

public class ExportarEvaluacionesQueryValidator : AbstractValidator<ExportarEvaluacionesQuery>
{
    public const int MaxDias = 366;

    public ExportarEvaluacionesQueryValidator()
    {
        RuleFor(x => x.Hasta).GreaterThanOrEqualTo(x => x.Desde).WithMessage("La fecha inicial no puede ser posterior a la final.");
        RuleFor(x => x)
            .Must(x => x.Hasta.DayNumber - x.Desde.DayNumber < MaxDias)
            .WithMessage($"El rango no puede superar {MaxDias} días.");
    }
}

public class ExportarEvaluacionesQueryHandler(IReadDbContext db, IHojaCalculo hojaCalculo)
    : IRequestHandler<ExportarEvaluacionesQuery, ArchivoExportado>
{
    private static readonly IReadOnlyDictionary<EstadoEvaluacionEnum, string> Estados = new Dictionary<EstadoEvaluacionEnum, string>
    {
        [EstadoEvaluacionEnum.EnGestion] = "En gestión",
        [EstadoEvaluacionEnum.Completada] = "Completada",
        [EstadoEvaluacionEnum.NoContactado] = "No contactado",
        [EstadoEvaluacionEnum.Rechazada] = "Rechazada",
    };

    public async Task<ArchivoExportado> Handle(ExportarEvaluacionesQuery request, CancellationToken cancellationToken)
    {
        var encuestas = db.Encuestas.Include(e => e.Preguntas).ThenInclude(p => p.Opciones);
        var encuesta = request.EncuestaId is { } id
            ? await encuestas.FirstOrDefaultAsync(e => e.Id == id, cancellationToken) ?? throw new NotFoundException("La encuesta", id)
            : await encuestas.FirstOrDefaultAsync(e => e.Estado == EstadoEncuestaEnum.Publicada, cancellationToken)
              ?? throw new ConflictException("No hay una encuesta de calidad publicada.");

        var (desdeUtc, hastaUtc) = ZonaHorariaOperativa.RangoUtc(request.Desde, request.Hasta);

        var filas = await db.EvaluacionesCalidad
            .Where(c => c.EncuestaId == encuesta.Id
                && c.Evento!.FechaHoraReporteUtc >= desdeUtc
                && c.Evento.FechaHoraReporteUtc < hastaUtc)
            .OrderBy(c => c.Evento!.FechaHoraReporteUtc)
            .Select(c => new
            {
                c.EventoId,
                c.Evento!.FechaHoraReporteUtc,
                Tipos = c.Evento.Tipos.Select(t => t.TipoEvento!.Nombre).Distinct().ToList(),
                Ficha = c.Evento.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Unidad!.Ficha).FirstOrDefault(),
                Denominacion = c.Evento.Unidades.Where(u => u.Rol == RolUnidadEventoEnum.Principal).Select(u => u.Denominacion!.Nombre).FirstOrDefault(),
                Tramo = c.Evento.Tramo != null ? c.Evento.Tramo.Nombre : null,
                c.NombreContacto,
                c.TelefonoContacto,
                c.Estado,
                Intentos = c.Intentos.Count(),
                c.CerradaUtc,
                c.CerradaPorUsuarioId,
                c.Observacion,
                Respuestas = c.Respuestas.Select(r => new { r.PreguntaEncuestaId, r.Valor, r.Texto, r.Comentario }).ToList()
            })
            
            .ToListAsync(cancellationToken);

        var usuarioIds = filas.Select(f => f.CerradaPorUsuarioId).OfType<int>().Distinct().ToList();
        var usuarios = await db.Usuarios
            .Where(u => usuarioIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, cancellationToken);

        var preguntas = encuesta.Preguntas.ToList();
        List<string> encabezado =
        [
            "Evento", "Fecha del evento", "Tipos", "Unidad", "Denominación", "Tramo",
            "Contacto", "Teléfono", "Estado", "Intentos", "Fecha de cierre", "Cerrada por",
        ];
        foreach (var p in preguntas)
        {
            encabezado.Add($"{p.Orden}. {p.Texto}");
            if (p.PermiteComentario) encabezado.Add($"{p.Orden}. Comentario");
        }
        encabezado.Add("Observación");

        var datos = new List<IReadOnlyList<string>> { encabezado };
        foreach (var f in filas)
        {
            var respuestas = f.Respuestas.ToDictionary(r => r.PreguntaEncuestaId);
            List<string> fila =
            [
                f.EventoId.ToString(),
                Fecha(f.FechaHoraReporteUtc),
                string.Join(", ", f.Tipos.Order()),
                f.Ficha ?? string.Empty,
                f.Denominacion ?? string.Empty,
                f.Tramo ?? string.Empty,
                f.NombreContacto ?? string.Empty,
                f.TelefonoContacto,
                Estados[f.Estado],
                f.Intentos.ToString(),
                f.CerradaUtc is { } cerrada ? Fecha(cerrada) : string.Empty,
                f.CerradaUtc is null ? string.Empty
                    : f.CerradaPorUsuarioId is { } u ? usuarios.GetValueOrDefault(u) ?? string.Empty : "Bot",
            ];
            foreach (var p in preguntas)
            {
                respuestas.TryGetValue(p.Id, out var r);
                fila.Add(r is null ? string.Empty : Describir(p, r.Valor, r.Texto));
                if (p.PermiteComentario) fila.Add(r?.Comentario ?? string.Empty);
            }
            fila.Add(f.Observacion ?? string.Empty);
            datos.Add(fila);
        }

        var contenido = hojaCalculo.Escribir([new HojaNueva($"Calidad v{encuesta.Version}", datos)]);
        return new ArchivoExportado(contenido, $"calidad_v{encuesta.Version}_{request.Desde:yyyyMMdd}-{request.Hasta:yyyyMMdd}.xlsx");
    }

    /// <summary>Escala: "5 - Bueno" (el número permite promediar en Excel); Sí/No; opción: su etiqueta.</summary>
    private static string Describir(PreguntaEncuesta pregunta, int? valor, string? texto) => pregunta.Tipo switch
    {
        TipoPreguntaEnum.Texto => texto ?? string.Empty,
        TipoPreguntaEnum.SiNo => valor == PreguntaEncuesta.ValorSi ? "Sí" : valor == PreguntaEncuesta.ValorNo ? "No" : string.Empty,
        TipoPreguntaEnum.Escala => pregunta.Opciones.FirstOrDefault(o => o.Valor == valor) is { } o ? $"{o.Valor} - {o.Etiqueta}" : valor?.ToString() ?? string.Empty,
        _ => pregunta.Opciones.FirstOrDefault(o => o.Valor == valor)?.Etiqueta ?? valor?.ToString() ?? string.Empty,
    };

    private static string Fecha(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaHorariaOperativa.Zona).ToString("dd/MM/yyyy HH:mm");
}
