using Domain.Entities.Historico;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Persona involucrada en un evento, asociada a uno de los vehículos del evento si iba en él
/// (conductor, pasajero) o sin vehículo (peatón...). Los datos quedan como foto del momento; el
/// vínculo con el maestro histórico es opcional y solo existe para personas identificadas.
/// </summary>
public class EventoCiudadanoInfo
{
    public int Id { get; private set; }

    public int EventoId { get; private set; }
    public virtual Evento? Evento { get; private set; }

    public RolCiudadanoEnum Rol { get; private set; }

    public DatosPersona Persona { get; private set; } = null!;

    /// <summary>Vehículo del mismo evento en que iba la persona. null = sin vehículo.</summary>
    public int? EventoVehiculoId { get; private set; }
    public virtual EventoVehiculoInfo? Vehiculo { get; private set; }

    public int? CiudadanoId { get; private set; }
    public virtual Ciudadano? Ciudadano { get; private set; }

    private readonly List<EventoCiudadanoTipoEvento> _tipos = new();

    /// <summary>
    /// Tipos atendidos a esta persona (p. ej. Seguridad a un peatón). Puede estar vacío: quien iba en
    /// un vehículo suele quedar cubierto por los tipos del vehículo.
    /// </summary>
    public IReadOnlyCollection<EventoCiudadanoTipoEvento> Tipos => _tipos.AsReadOnly();

    // Requerido por EF Core
    private EventoCiudadanoInfo() { }

    /// <summary>Conductor y pasajero van siempre en un vehículo del evento.</summary>
    public static bool RequiereVehiculo(RolCiudadanoEnum rol)
        => rol is RolCiudadanoEnum.Conductor or RolCiudadanoEnum.Pasajero;

    /// <summary>Un peatón nunca va en un vehículo; paciente u otro pueden ir o no.</summary>
    public static bool AdmiteVehiculo(RolCiudadanoEnum rol) => rol != RolCiudadanoEnum.Peaton;

    internal static EventoCiudadanoInfo Crear(
        RolCiudadanoEnum rol,
        DatosPersona persona,
        EventoVehiculoInfo? vehiculo,
        Ciudadano? ciudadano,
        IReadOnlyCollection<int> tipoEventoIds)
    {
        if (!Enum.IsDefined(rol)) throw new DomainException("El rol del ciudadano no es válido.");
        if (vehiculo is null && RequiereVehiculo(rol))
            throw new DomainException($"El {Descripcion(rol)} debe estar asociado a un vehículo del evento.");
        if (vehiculo is not null && !AdmiteVehiculo(rol))
            throw new DomainException("Un peatón no puede estar asociado a un vehículo.");

        var participacion = new EventoCiudadanoInfo
        {
            Rol = rol,
            // Copia propia: cada participación debe tener su instancia (el mismo DatosPersona
            // podría reutilizarse para varias personas y la persistencia los asocia por referencia)
            Persona = persona with { },
            Vehiculo = vehiculo,
            Ciudadano = ciudadano,
            CiudadanoId = ciudadano is { Id: > 0 } ? ciudadano.Id : null
        };
        participacion._tipos.AddRange(tipoEventoIds.Select(id => new EventoCiudadanoTipoEvento(id)));
        return participacion;
    }

    private static string Descripcion(RolCiudadanoEnum rol) => rol == RolCiudadanoEnum.Conductor ? "conductor" : "pasajero";
}
