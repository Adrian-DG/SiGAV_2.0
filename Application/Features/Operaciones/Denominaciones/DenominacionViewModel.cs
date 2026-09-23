using Domain.Enums;

namespace Application.Features.Operaciones.Denominaciones;

public record DenominacionViewModel(
    int Id,
    string Nombre,
    int NivelDenominacionId,
    string NivelDenominacion,
    JerarquiaEnum Jerarquia,
    int TramoId,
    string Tramo);

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
