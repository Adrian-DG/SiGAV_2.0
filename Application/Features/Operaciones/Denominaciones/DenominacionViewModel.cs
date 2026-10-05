using Domain.Enums;

namespace Application.Features.Operaciones.Denominaciones;

/// <param name="UnidadId">Unidad activa que la usa ahora (null: libre). UnidadFicha: su ficha.</param>
public record DenominacionViewModel(
    int Id,
    string Nombre,
    int NivelDenominacionId,
    string NivelDenominacion,
    JerarquiaEnum Jerarquia,
    int TramoId,
    string Tramo,
    int? UnidadId,
    string? UnidadFicha);

public record DenominacionDetalleViewModel(
    int Id,
    string Nombre,
    int NivelDenominacionId,
    string NivelDenominacion,
    JerarquiaEnum Jerarquia,
    int TramoId,
    string Tramo,
    IReadOnlyList<RegionMacroEnum> RegionesMacro,
    IReadOnlyList<AsignacionViewModel> RegionesAsistencia,
    IReadOnlyList<AsignacionViewModel> TramosAsignados);

public record AsignacionViewModel(int Id, string Nombre);
