using Application.Features.Operaciones.Denominaciones;
using Domain.Enums;

namespace Application.Features.Estadisticas;

/// <summary>
/// Eventos atendidos dentro del alcance de la sesión, desglosados región → tramo → unidad.
/// </summary>
public record EstadisticasEventosViewModel(
    AlcanceViewModel Alcance,
    DateOnly Desde,
    DateOnly Hasta,
    ResumenEventosViewModel Resumen,
    IReadOnlyList<ZonaEstadisticaViewModel> Zonas,
    MetasLlegadaViewModel Metas);

/// <summary>Meta de llegada (minutos desde el reporte) según la categoría del evento.</summary>
public record MetasLlegadaViewModel(int AccidenteMinutos, int AsistenciaMinutos);

/// <summary>Qué cubre la consulta. Jerarquia null = sin restricción (front desk).</summary>
public record AlcanceViewModel(
    JerarquiaEnum? Jerarquia,
    int? DenominacionId,
    string? Denominacion,
    IReadOnlyList<RegionMacroEnum> RegionesMacro,
    IReadOnlyList<AsignacionViewModel> Zonas,
    IReadOnlyList<AsignacionViewModel> Tramos);

/// <summary>
/// TotalEventos y PorCategoria cuentan eventos distintos; en PorTipo un evento con varios
/// tipos cuenta una vez en cada uno de ellos.
/// </summary>
public record ResumenEventosViewModel(
    int TotalEventos,
    IReadOnlyList<CategoriaTotalViewModel> PorCategoria,
    IReadOnlyList<TipoEventoTotalViewModel> PorTipo,
    TiemposRespuestaViewModel Tiempos);

/// <summary>
/// Tiempos de respuesta en minutos (null si no hay eventos medibles).
/// - Llegada: reporte → llegada de la unidad. Solo eventos despachados (no los que reporta el
///   propio agente en campo, que ya está en el lugar).
/// - Atención: llegada → completado.
/// - Cumplimiento: llegadas dentro de la meta de su categoría sobre las medidas más los
///   pendientes vencidos (sin llegada y con la meta ya superada), que también incumplen.
/// </summary>
public record TiemposRespuestaViewModel(
    int LlegadasMedidas,
    double? LlegadaPromedioMinutos,
    double? LlegadaMedianaMinutos,
    double? LlegadaP90Minutos,
    int AtencionesMedidas,
    double? AtencionPromedioMinutos,
    double? AtencionMedianaMinutos,
    int DentroDeMeta,
    int FueraDeMeta,
    int PendientesVencidos,
    double? CumplimientoPorcentaje);

public record CategoriaTotalViewModel(CategoriaEventoEnum Categoria, int Total);

public record TipoEventoTotalViewModel(int TipoEventoId, string TipoEvento, CategoriaEventoEnum Categoria, int Total);

public record ZonaEstadisticaViewModel(
    int ZonaId,
    string Zona,
    RegionMacroEnum RegionMacro,
    ResumenEventosViewModel Resumen,
    IReadOnlyList<TramoEstadisticaViewModel> Tramos);

public record TramoEstadisticaViewModel(
    int TramoId,
    string Tramo,
    ResumenEventosViewModel Resumen,
    IReadOnlyList<UnidadEstadisticaViewModel> Unidades);

/// <summary>
/// Ficha con la denominación que tenía al atender los eventos: una misma ficha puede
/// aparecer con dos denominaciones si fue reasignada dentro del período.
/// </summary>
public record UnidadEstadisticaViewModel(
    int UnidadId,
    string Ficha,
    int DenominacionId,
    string Denominacion,
    ResumenEventosViewModel Resumen);
