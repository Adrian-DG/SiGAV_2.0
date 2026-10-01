using Domain.Entities.Historico;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Vehículo involucrado en un evento. Varias personas pueden estar asociadas a él (conductor y
/// pasajeros) y también puede no tener ninguna (p. ej. un vehículo abandonado). Los datos quedan
/// como foto del momento; el vínculo con el maestro histórico es opcional.
/// </summary>
public class EventoVehiculoInfo
{
    public int Id { get; private set; }

    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public DatosVehiculo Datos { get; private set; } = null!;

    public int? VehiculoHistoricoId { get; private set; }
    public virtual Vehiculo? VehiculoHistorico { get; private set; }

    // Requerido por EF Core
    private EventoVehiculoInfo() { }

    internal static EventoVehiculoInfo Crear(DatosVehiculo datos, Vehiculo? vehiculoHistorico)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (vehiculoHistorico is not null && vehiculoHistorico.Placa != datos.Placa)
            throw new DomainException("El vehículo histórico no corresponde a la placa registrada.");

        return new EventoVehiculoInfo
        {
            // Copia propia: la persistencia asocia los owned types por referencia
            Datos = datos with { },
            VehiculoHistorico = vehiculoHistorico,
            VehiculoHistoricoId = vehiculoHistorico is { Id: > 0 } ? vehiculoHistorico.Id : null
        };
    }
}
