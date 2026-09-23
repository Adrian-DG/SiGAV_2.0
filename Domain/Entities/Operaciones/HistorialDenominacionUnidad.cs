using System.ComponentModel.DataAnnotations.Schema;
using Domain.Abstraction;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Registro de auditoría de un cambio de denominación de una unidad. Lo crea únicamente el
/// agregado <see cref="Unidad"/> al asignar o liberar una denominación; es inmutable.
/// </summary>
[Table("historial_denominacion_unidad", Schema = "operaciones")]
public class HistorialDenominacionUnidad : IRegistroInmutable
{
    public long Id { get; private set; }

    public int UnidadId { get; private set; }

    public TipoCambioDenominacionEnum TipoCambio { get; private set; }

    public int? DenominacionAnteriorId { get; private set; }
    public virtual Denominacion? DenominacionAnterior { get; private set; }

    public int? DenominacionNuevaId { get; private set; }
    public virtual Denominacion? DenominacionNueva { get; private set; }

    /// <summary>
    /// En una liberación: la unidad que tomó la denominación. En una asignación o
    /// reasignación: la unidad a la que se le quitó (si la tenía otra).
    /// </summary>
    public int? UnidadRelacionadaId { get; private set; }
    public virtual Unidad? UnidadRelacionada { get; private set; }

    /// <summary>Fecha y hora del cambio (UTC).</summary>
    public DateTime FechaUtc { get; private set; }

    /// <summary>Usuario de la aplicación web que realizó el cambio.</summary>
    public int UsuarioId { get; private set; }

    public string? Observacion { get; private set; }

    // Requerido por EF Core
    private HistorialDenominacionUnidad() { }

    internal static HistorialDenominacionUnidad Registrar(
        TipoCambioDenominacionEnum tipo,
        int? denominacionAnteriorId,
        Denominacion? nueva,
        Unidad? relacionada,
        AutorCambio autor) => new()
    {
        TipoCambio = tipo,
        DenominacionAnteriorId = denominacionAnteriorId,
        // Se asignan navegaciones además de Ids: la denominación nueva o la unidad relacionada
        // pueden no existir aún (Id = 0) y EF Core resuelve la FK al guardar.
        DenominacionNueva = nueva,
        DenominacionNuevaId = nueva is { Id: > 0 } ? nueva.Id : null,
        UnidadRelacionada = relacionada,
        UnidadRelacionadaId = relacionada is { Id: > 0 } ? relacionada.Id : null,
        FechaUtc = autor.FechaUtc,
        UsuarioId = autor.UsuarioId,
        Observacion = autor.Observacion
    };
}
