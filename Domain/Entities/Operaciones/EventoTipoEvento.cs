namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipo de evento atendido. Una fila por tipo y por quién lo recibió: uno de los vehículos del
/// evento, una de sus personas o el evento en general (ambos null). Los tipos del evento son los de
/// todas sus filas (<see cref="Evento.TipoEventoIds"/>). Parte del agregado: solo se crea a través de
/// <see cref="Evento.Registrar"/>, <see cref="Evento.AgregarVehiculo"/> y <see cref="Evento.AgregarCiudadano"/>.
/// </summary>
public class EventoTipoEvento
{
    public int Id { get; private set; }

    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public int TipoEventoId { get; private set; }
    public virtual TipoEvento? TipoEvento { get; private set; }

    public int? EventoVehiculoId { get; private set; }
    public virtual EventoVehiculoInfo? Vehiculo { get; private set; }

    public int? EventoCiudadanoId { get; private set; }
    public virtual EventoCiudadanoInfo? Ciudadano { get; private set; }

    /// <summary>Atendido al evento en general, no a un vehículo o persona en particular.</summary>
    public bool EsGeneral => Vehiculo is null && Ciudadano is null && EventoVehiculoId is null && EventoCiudadanoId is null;

    // Requerido por EF Core
    private EventoTipoEvento() { }

    internal EventoTipoEvento(int tipoEventoId, EventoVehiculoInfo? vehiculo = null, EventoCiudadanoInfo? ciudadano = null)
    {
        TipoEventoId = tipoEventoId;
        Vehiculo = vehiculo;
        Ciudadano = ciudadano;
    }
}
