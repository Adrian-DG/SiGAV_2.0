using System.Linq.Expressions;
using Domain.Entities.Operaciones;

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
    bool IsActive)
{
    /// <summary>Proyección para las Queries (se traduce a SQL).</summary>
    internal static readonly Expression<Func<Unidad, UnidadViewModel>> Proyeccion = u => new UnidadViewModel(
        u.Id,
        u.Ficha,
        u.Placa,
        u.DenominacionId,
        u.Denominacion != null ? u.Denominacion.Nombre : string.Empty,
        u.Denominacion != null ? u.Denominacion.NivelDenominacionId : null,
        u.Denominacion != null ? u.Denominacion.Nivel!.Nombre : string.Empty,
        u.Denominacion != null ? u.Denominacion.TramoId : null,
        u.Denominacion != null ? u.Denominacion.Tramo!.Nombre : string.Empty,
        u.EstaDisponible,
        u.IsActive);
}

public record UnidadAutoCompleteViewModel(
    int UnidadId,
    string Ficha,
    string? Placa,
    string Denominacion,
    string Tramo,
    bool EstaDisponible);
