using Domain.Enums;

namespace Application.Features.Operaciones.Zonas;

/// <param name="Region">Región a la que pertenece la zona (Este, Norte o Sur).</param>
public record ZonaViewModel(
    int Id,
    string Nombre,
    RegionMacroEnum Region,
    bool IsActive,
    IReadOnlyList<TramoDeZonaViewModel> Tramos);

/// <param name="Denominaciones">Denominaciones activas que tienen este tramo como propio.</param>
public record TramoDeZonaViewModel(int Id, string Nombre, bool IsActive, int Denominaciones);

/// <param name="Denominaciones">Denominaciones activas que tienen este tramo como propio.</param>
public record TramoViewModel(
    int Id,
    string Nombre,
    int ZonaId,
    string Zona,
    RegionMacroEnum Region,
    bool IsActive,
    int Denominaciones);
