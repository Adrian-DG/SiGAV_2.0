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
    IReadOnlyList<RegionEstadisticaViewModel> Regiones);

/// <summary>Qué cubre la consulta. Jerarquia null = sin restricción (front desk).</summary>
public record AlcanceViewModel(
    JerarquiaEnum? Jerarquia,
    int? DenominacionId,
    string? Denominacion,
    IReadOnlyList<RegionMacroEnum> RegionesMacro,
    IReadOnlyList<AsignacionViewModel> RegionesAsistencia,
    IReadOnlyList<AsignacionViewModel> Tramos);

/// <summary>
/// TotalEventos y PorCategoria cuentan eventos distintos; en PorTipo un evento con varios
/// tipos cuenta una vez en cada uno de ellos.
/// </summary>
public record ResumenEventosViewModel(
    int TotalEventos,
    IReadOnlyList<CategoriaTotalViewModel> PorCategoria,
    IReadOnlyList<TipoEventoTotalViewModel> PorTipo);

public record CategoriaTotalViewModel(CategoriaEventoEnum Categoria, int Total);

public record TipoEventoTotalViewModel(int TipoEventoId, string TipoEvento, CategoriaEventoEnum Categoria, int Total);

public record RegionEstadisticaViewModel(
    int RegionAsistenciaId,
    string Region,
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
