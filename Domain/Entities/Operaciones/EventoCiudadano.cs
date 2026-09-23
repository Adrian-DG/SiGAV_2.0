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
public class EventoCiudadano
{
    public int Id { get; private set; }

    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public RolCiudadanoEnum Rol { get; private set; }

    public DatosPersona Persona { get; private set; } = null!;

    /// <summary>null = sin vehículo (peatón, paciente...).</summary>
    public DatosVehiculo? Vehiculo { get; private set; }

    public int? CiudadanoId { get; private set; }
    public virtual Ciudadano? Ciudadano { get; private set; }

    public int? VehiculoHistoricoId { get; private set; }
    public virtual Vehiculo? VehiculoHistorico { get; private set; }

    // Requerido por EF Core
    private EventoCiudadano() { }

    internal static EventoCiudadano Crear(
        RolCiudadanoEnum rol,
        DatosPersona persona,
        DatosVehiculo? vehiculo,
        Ciudadano? ciudadano,
        Vehiculo? vehiculoHistorico)
    {
        if (!Enum.IsDefined(rol)) throw new DomainException("El rol del ciudadano no es válido.");
        if (vehiculoHistorico is not null && vehiculo is null)
            throw new DomainException("No se puede vincular un vehículo histórico sin registrar los datos del vehículo.");

        return new EventoCiudadano
        {
            Rol = rol,
            // Copias propias: cada participación debe tener su instancia (el mismo DatosPersona
            // podría reutilizarse para varias personas y la persistencia los asocia por referencia)
            Persona = persona with { },
            Vehiculo = vehiculo is null ? null : vehiculo with { },
            Ciudadano = ciudadano,
            CiudadanoId = ciudadano is { Id: > 0 } ? ciudadano.Id : null,
            VehiculoHistorico = vehiculoHistorico,
            VehiculoHistoricoId = vehiculoHistorico is { Id: > 0 } ? vehiculoHistorico.Id : null
        };
    }
}
