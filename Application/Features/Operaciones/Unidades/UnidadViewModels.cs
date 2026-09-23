namespace Application.Features.Operaciones.Unidades;

public record UnidadViewModel(
    int Id,
    string Ficha,
    string? Placa,
    int? DenominacionId,
    string Denominacion,
    int? NivelDenominacionId,
    string NivelDenominacion,
    int? TramoId,
    string Tramo,
    bool EstaDisponible,
    bool IsActive);

public record UnidadAutoCompleteViewModel(
    int UnidadId,
    string Ficha,
    string? Placa,
    string Denominacion,
    string Tramo,
    bool EstaDisponible);
