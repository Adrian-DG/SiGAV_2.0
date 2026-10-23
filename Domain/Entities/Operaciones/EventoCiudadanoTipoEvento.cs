namespace Domain.Entities.Operaciones;

/// <summary>
/// Tipo de evento atendido a una persona (p. ej. Seguridad a un peatón, atención a un pasajero).
/// Parte del agregado: se crea a través de <see cref="Evento.AgregarCiudadano"/> y siempre es uno de
/// los tipos del evento.
/// </summary>
public class EventoCiudadanoTipoEvento
{
    public int EventoCiudadanoId { get; private set; }
    public virtual EventoCiudadanoInfo? EventoCiudadano { get; private set; }

    public int TipoEventoId { get; private set; }
    public virtual TipoEvento? TipoEvento { get; private set; }

    // Requerido por EF Core
    private EventoCiudadanoTipoEvento() { }

    internal EventoCiudadanoTipoEvento(int tipoEventoId) => TipoEventoId = tipoEventoId;
}
