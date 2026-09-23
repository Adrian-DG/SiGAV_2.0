using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Participación de una unidad en un evento. Denominación y nivel quedan como foto del momento
/// del evento (las estadísticas no cambian si luego se reasigna la denominación o su nivel).
/// </summary>
public class EventoUnidad
{
    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public int UnidadId { get; private set; }
    public virtual Unidad? Unidad { get; private set; }

    public int DenominacionId { get; private set; }
    public virtual Denominacion? Denominacion { get; private set; }

    public int NivelDenominacionId { get; private set; }
    public virtual NivelDenominacion? NivelDenominacion { get; private set; }

    /// <summary>Agente que operaba la unidad (el de la sesión que reporta, en la app).</summary>
    public int AgenteId { get; private set; }
    public virtual Agente? Agente { get; private set; }

    public RolUnidadEventoEnum Rol { get; private set; }

    // Requerido por EF Core
    private EventoUnidad() { }

    /// <summary>
    /// Toma la foto de denominación y nivel desde la unidad: el cliente no los envía, así no
    /// pueden quedar inconsistentes. Requiere la unidad con su denominación cargada.
    /// </summary>
    internal static EventoUnidad Crear(Unidad unidad, int agenteId, RolUnidadEventoEnum rol)
    {
        if (!unidad.IsActive) throw new DomainException($"La unidad '{unidad.Ficha}' está desactivada.");
        if (unidad.DenominacionId is not { } denominacionId || unidad.Denominacion is null)
            throw new DomainException($"La unidad '{unidad.Ficha}' no tiene denominación asignada.");
        if (agenteId <= 0) throw new DomainException("El agente de la unidad es requerido.");
        if (!Enum.IsDefined(rol)) throw new DomainException("El rol de la unidad no es válido.");

        return new EventoUnidad
        {
            UnidadId = unidad.Id,
            DenominacionId = denominacionId,
            NivelDenominacionId = unidad.Denominacion.NivelDenominacionId,
            AgenteId = agenteId,
            Rol = rol
        };
    }

    internal void CambiarRol(RolUnidadEventoEnum rol) => Rol = rol;
}
