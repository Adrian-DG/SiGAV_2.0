using Domain.Enums;

namespace Application.Features.Estadisticas;

/// <summary>
/// Metas de llegada (minutos desde el reporte hasta que la unidad llega) por categoría. Se
/// configuran en "Estadisticas:MetasLlegada"; si un evento tiene tipos de ambas categorías se
/// aplica la de accidente (la más exigente).
/// </summary>
public sealed class MetasLlegadaOptions
{
    public const string SectionName = "Estadisticas:MetasLlegada";

    public int AccidenteMinutos { get; set; } = 15;
    public int AsistenciaMinutos { get; set; } = 30;

    public int MetaPara(bool esAccidente) => esAccidente ? AccidenteMinutos : AsistenciaMinutos;
}

/// <summary>Tiempos de un evento, para calcular <see cref="TiemposRespuestaViewModel"/>.</summary>
public sealed record TiempoEvento(
    int EventoId,
    DateTime ReporteUtc,
    DateTime? LlegadaUtc,
    DateTime? CompletadoUtc,
    bool Despachado,
    int MetaMinutos);

public static class TiemposRespuesta
{
    public static readonly TiemposRespuestaViewModel Vacio = new(0, null, null, null, 0, null, null, 0, 0, 0, null);

    public static TiemposRespuestaViewModel Calcular(IEnumerable<TiempoEvento> eventos, DateTime ahoraUtc)
    {
        var llegadas = new List<double>();
        var atenciones = new List<double>();
        int dentro = 0, fuera = 0, vencidos = 0;

        foreach (var e in eventos)
        {
            if (e.LlegadaUtc is { } llegada)
            {
                if (e.CompletadoUtc is { } completado)
                    atenciones.Add((completado - llegada).TotalMinutes);

                if (!e.Despachado) continue;

                var minutos = (llegada - e.ReporteUtc).TotalMinutes;
                llegadas.Add(minutos);
                if (minutos <= e.MetaMinutos) dentro++;
                else fuera++;
            }
            else if (e.Despachado && e.CompletadoUtc is null && (ahoraUtc - e.ReporteUtc).TotalMinutes > e.MetaMinutos)
            {
                vencidos++;
            }
        }

        var base_ = dentro + fuera + vencidos;
        return new TiemposRespuestaViewModel(
            llegadas.Count,
            Promedio(llegadas),
            Percentil(llegadas, 0.5),
            Percentil(llegadas, 0.9),
            atenciones.Count,
            Promedio(atenciones),
            Percentil(atenciones, 0.5),
            dentro,
            fuera,
            vencidos,
            base_ == 0 ? null : Math.Round(dentro * 100.0 / base_, 1));
    }

    private static double? Promedio(List<double> valores)
        => valores.Count == 0 ? null : Math.Round(valores.Average(), 1);

    /// <summary>Percentil con interpolación lineal entre los valores ordenados.</summary>
    private static double? Percentil(List<double> valores, double p)
    {
        if (valores.Count == 0) return null;

        var ordenados = valores.Order().ToList();
        var posicion = (ordenados.Count - 1) * p;
        var bajo = (int)Math.Floor(posicion);
        var alto = (int)Math.Ceiling(posicion);
        var valor = ordenados[bajo] + (ordenados[alto] - ordenados[bajo]) * (posicion - bajo);
        return Math.Round(valor, 1);
    }

    /// <summary>Despachado = la unidad tuvo que trasladarse (no lo reportó el agente ya en el lugar).</summary>
    public static bool EsDespachado(CanalReporteEnum canal) => canal != CanalReporteEnum.AgenteCampo;
}
