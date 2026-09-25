using Domain.Entities.Historico;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Persona involucrada en un evento, con su vehículo si aplica (un peatón o un paciente no
/// tienen). Los datos quedan como foto del momento; el vínculo con los maestros históricos
/// (Ciudadano, Vehiculo) es opcional y solo existe para personas y vehículos identificados.
/// </summary>
public class EventoCiudadanoInfo
{
    public int Id { get; private set; }

    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public int TipoEventoId { get; private set; }
    public virtual TipoEvento? TipoEvento { get; private set; }
    public RolCiudadanoEnum Rol { get; private set; }
    public int? CiudadanoId { get; private set; }
    public virtual Ciudadano? Ciudadano { get; private set; }
    public int? VehiculoId { get; private set; }
    public virtual Vehiculo? Vehiculo { get; private set; }

    // Requerido por EF Core
    private EventoCiudadanoInfo() { }

    internal static EventoCiudadanoInfo Crear(
        int TipoEventoId,
        RolCiudadanoEnum rol,
        int? ciudadanoId,
        int? vehiculoId)
    {
        if (!Enum.IsDefined(rol)) throw new DomainException("El rol del ciudadano no es válido.");
        
        return new EventoCiudadanoInfo
        {
            TipoEventoId = TipoEventoId,
            Rol = rol,
            CiudadanoId = ciudadanoId is { } ? ciudadanoId : null,
            VehiculoId = vehiculoId is { } ? vehiculoId : null
        };
    }
}
