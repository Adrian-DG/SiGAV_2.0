using System.Globalization;
using Application.Common;
using Application.Contracts;
using Application.Features.Calidad;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Agentes;

/// <summary>
/// Excel con los eventos atendidos por cada agente en el rango (días operativos de RD, por fecha de
/// reporte): asistencias, accidentes y total. Con <see cref="Mensual"/> agrega una hoja con el conteo
/// de cada agente por mes. Opcionalmente se limita a un área operativa o institución.
/// </summary>
/// <remarks>
/// Un agente atiende el evento cuando operaba la unidad principal o una de apoyo que llegó (un apoyo
/// solicitado que aún no llega no cuenta). Se cuentan eventos distintos y no anulados: un evento con
/// tipos de ambas categorías suma en asistencias y en accidentes, pero una sola vez en el total.
/// </remarks>
public record ExportarEventosPorAgenteQuery(
    DateOnly Desde,
    DateOnly Hasta,
    bool Mensual = false,
    AreaOperativaEnum? AreaOperativa = null,
    InstitucionEnum? Institucion = null) : IRequest<ArchivoExportado>;

public class ExportarEventosPorAgenteQueryValidator : AbstractValidator<ExportarEventosPorAgenteQuery>
{
    public const int MaxDias = 366;

    public ExportarEventosPorAgenteQueryValidator()
    {
        RuleFor(x => x.Hasta).GreaterThanOrEqualTo(x => x.Desde).WithMessage("La fecha inicial no puede ser posterior a la final.");
        RuleFor(x => x)
            .Must(x => x.Hasta.DayNumber - x.Desde.DayNumber < MaxDias)
            .WithMessage($"El rango no puede superar {MaxDias} días.");
        RuleFor(x => x.AreaOperativa).IsInEnum().When(x => x.AreaOperativa.HasValue).WithMessage("El área operativa no es válida.");
        RuleFor(x => x.Institucion).IsInEnum().When(x => x.Institucion.HasValue).WithMessage("La institución no es válida.");
    }
}

public class ExportarEventosPorAgenteQueryHandler(IReadDbContext db, IHojaCalculo hojaCalculo)
    : IRequestHandler<ExportarEventosPorAgenteQuery, ArchivoExportado>
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    private static readonly IReadOnlyDictionary<AreaOperativaEnum, string> Areas = new Dictionary<AreaOperativaEnum, string>
    {
        [AreaOperativaEnum.AsistenciaVial] = "Asistencia vial",
        [AreaOperativaEnum.GestionOperativa] = "Gestión operativa",
        [AreaOperativaEnum.SeguridadCiudadana] = "Seguridad ciudadana",
        [AreaOperativaEnum.Taller] = "Taller",
        [AreaOperativaEnum.Gruas] = "Grúas",
        [AreaOperativaEnum.PreHospitalaria] = "Prehospitalaria",
        [AreaOperativaEnum.Rescate] = "Rescate",
    };

    private sealed record Fila(int EventoId, int AgenteId, DateTime FechaUtc, CategoriaEventoEnum? Categoria);
    private sealed record Conteo(int Asistencias, int Accidentes, int Total);

    public async Task<ArchivoExportado> Handle(ExportarEventosPorAgenteQuery request, CancellationToken cancellationToken)
    {
        var (desdeUtc, hastaUtc) = ZonaHorariaOperativa.RangoUtc(request.Desde, request.Hasta);

        var participaciones = db.EventoUnidades.Where(eu =>
            eu.Rol != RolUnidadEventoEnum.ApoyoSolicitado
            && eu.Evento!.IsActive
            && eu.Evento.FechaHoraReporteUtc >= desdeUtc
            && eu.Evento.FechaHoraReporteUtc < hastaUtc);
        if (request.AreaOperativa is { } area)
            participaciones = participaciones.Where(eu => eu.Agente!.AreaOperativa == area);
        if (request.Institucion is { } institucion)
            participaciones = participaciones.Where(eu => eu.Agente!.Institucion == institucion);

        // Filas livianas (un evento puede repetir agente en varias unidades y tipo en varias filas);
        // se quitan duplicados y se agrega en memoria
        var filas = (await (
                from eu in participaciones
                join et in db.EventoTiposEvento on eu.EventoId equals et.EventoId into tipos
                from et in tipos.DefaultIfEmpty()
                select new Fila(eu.EventoId, eu.AgenteId, eu.Evento!.FechaHoraReporteUtc, (CategoriaEventoEnum?)et!.TipoEvento!.Categoria))
            .ToListAsync(cancellationToken))
            .Distinct()
            .ToList();

        var agenteIds = filas.Select(f => f.AgenteId).Distinct().ToList();
        var agentes = await db.Agentes
            .Where(a => agenteIds.Contains(a.Id))
            .Select(a => new
            {
                a.Id,
                a.Identificacion,
                a.Nombre,
                a.Apellido,
                a.Institucion,
                Rango = a.Institucion == InstitucionEnum.ARD ? a.Rango!.NombreArmada : a.Rango!.Nombre,
                a.AreaOperativa,
            })
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var orden = agentes.Values
            .OrderBy(a => a.Apellido, StringComparer.Create(Es, true))
            .ThenBy(a => a.Nombre, StringComparer.Create(Es, true))
            .Select(a => a.Id)
            .ToList();

        List<string> DatosAgente(int id)
        {
            var a = agentes[id];
            return [a.Identificacion, $"{a.Apellido} {a.Nombre}", a.Rango ?? string.Empty, a.Institucion.ToString(), Areas.GetValueOrDefault(a.AreaOperativa, string.Empty)];
        }
        string[] encabezadoAgente = ["Cédula", "Agente", "Rango", "Institución", "Área operativa"];
        string[] encabezadoConteo = ["Asistencias", "Accidentes", "Total eventos"];

        // ------------------------------------------------ Resumen del rango

        var porAgente = filas.GroupBy(f => f.AgenteId).ToDictionary(g => g.Key, g => Contar(g));
        List<IReadOnlyList<string>> resumen = [[.. encabezadoAgente, .. encabezadoConteo]];
        foreach (var id in orden)
            resumen.Add([.. DatosAgente(id), .. Celdas(porAgente[id])]);

        // Total general: eventos distintos (un evento con dos agentes cuenta una vez)
        var filaTotal = resumen.Count;
        resumen.Add(["", "Total general (eventos distintos)", "", "", "", .. Celdas(Contar(filas))]);

        var numericasResumen = Enumerable.Range(encabezadoAgente.Length, encabezadoConteo.Length).ToHashSet();
        var hojas = new List<HojaNueva>
        {
            new("Resumen", resumen, numericasResumen, new HashSet<int> { filaTotal }),
        };

        // ------------------------------------------------ Conteo mensual

        if (request.Mensual)
        {
            List<IReadOnlyList<string>> mensual = [[.. encabezadoAgente, "Mes", .. encabezadoConteo]];
            var porAgenteMes = filas
                .GroupBy(f => (f.AgenteId, Mes: MesOperativo(f.FechaUtc)))
                .ToDictionary(g => g.Key, g => Contar(g));

            foreach (var id in orden)
                foreach (var (_, mes) in porAgenteMes.Keys.Where(k => k.AgenteId == id).OrderBy(k => k.Mes))
                    mensual.Add([.. DatosAgente(id), NombreMes(mes), .. Celdas(porAgenteMes[(id, mes)])]);

            var numericasMes = Enumerable.Range(encabezadoAgente.Length + 1, encabezadoConteo.Length).ToHashSet();
            hojas.Add(new HojaNueva("Por mes", mensual, numericasMes));
        }

        // ------------------------------------------------ Parámetros (qué se filtró)

        hojas.Add(new HojaNueva("Parámetros",
        [
            ["Parámetro", "Valor"],
            ["Desde", request.Desde.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)],
            ["Hasta", request.Hasta.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)],
            ["Área operativa", request.AreaOperativa is { } a ? Areas.GetValueOrDefault(a, a.ToString()) : "Todas"],
            ["Institución", request.Institucion?.ToString() ?? "Todas"],
            ["Criterio", "Eventos no anulados por fecha de reporte, donde el agente operaba la unidad principal o una de apoyo. Un evento con asistencia y accidente suma en ambas columnas y una vez en el total."],
        ]));

        var contenido = hojaCalculo.Escribir(hojas);
        return new ArchivoExportado(contenido, $"eventos_por_agente_{request.Desde:yyyyMMdd}-{request.Hasta:yyyyMMdd}.xlsx");
    }

    private static Conteo Contar(IEnumerable<Fila> filas)
    {
        var lista = filas as IReadOnlyCollection<Fila> ?? filas.ToList();
        int Eventos(CategoriaEventoEnum categoria) => lista.Where(f => f.Categoria == categoria).Select(f => f.EventoId).Distinct().Count();
        return new Conteo(
            Eventos(CategoriaEventoEnum.ASISTENCIA),
            Eventos(CategoriaEventoEnum.ACCIDENTE),
            lista.Select(f => f.EventoId).Distinct().Count());
    }

    private static string[] Celdas(Conteo c) => [c.Asistencias.ToString(CultureInfo.InvariantCulture), c.Accidentes.ToString(CultureInfo.InvariantCulture), c.Total.ToString(CultureInfo.InvariantCulture)];

    /// <summary>Primer día del mes operativo (RD) del instante UTC.</summary>
    private static DateOnly MesOperativo(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaHorariaOperativa.Zona);
        return new DateOnly(local.Year, local.Month, 1);
    }

    /// <summary>"2026-11 noviembre": ordena bien como texto y se lee fácil.</summary>
    private static string NombreMes(DateOnly mes) => $"{mes:yyyy-MM} {mes.ToString("MMMM", Es)}";
}
