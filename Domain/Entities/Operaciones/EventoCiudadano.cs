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

    private readonly List<EventoTipoEvento> _tipos = new();

    /// <summary>
    /// Tipos atendidos a esta persona (p. ej. Seguridad a un peatón). Puede estar vacío: quien iba en
    /// un vehículo suele quedar cubierto por los tipos del vehículo. Son filas de <see cref="Evento.Tipos"/>:
    /// se agregan con <see cref="Evento.AgregarCiudadano"/>.
    /// </summary>
    public IReadOnlyCollection<EventoTipoEvento> Tipos => _tipos.AsReadOnly();

    /// <summary>
    /// Atención prehospitalaria (ambulancia); null = no la hubo. Puede tenerla cualquier rol: el conductor
    /// o el peatón herido en un accidente es justo a quien atiende la unidad alfa.
    /// </summary>
    public AtencionPrehospitalaria? Atencion { get; private set; }

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
        Ciudadano? ciudadano)
    {
        var participacion = new EventoCiudadanoInfo();
        participacion.AsignarDatos(rol, persona, vehiculo, ciudadano);
        return participacion;
    }

    /// <summary>
    /// La unidad principal reenvió a esta persona al completar el evento (<see cref="Evento.ReemplazarDatos"/>):
    /// se actualizan sus datos y se conserva su atención prehospitalaria.
    /// </summary>
    internal void Actualizar(RolCiudadanoEnum rol, DatosPersona persona, EventoVehiculoInfo? vehiculo, Ciudadano? ciudadano)
        => AsignarDatos(rol, persona, vehiculo, ciudadano);

    /// <summary>Se reemplazaron los vehículos y tipos del evento: la persona queda sin ellos hasta que se reenvíe.</summary>
    internal void Desvincular()
    {
        Vehiculo = null;
        EventoVehiculoId = null;
        _tipos.Clear();
    }

    /// <summary>
    /// ¿Es la misma persona? Por identificación si ambas la tienen; si no, por nombre y apellido
    /// (sin distinguir mayúsculas). Una persona desconocida no coincide con nadie.
    /// </summary>
    internal bool EsLaMismaPersona(DatosPersona otra)
    {
        if (Persona.Identificacion is { } identificacion && otra.Identificacion is { } otraIdentificacion)
            return identificacion == otraIdentificacion;

        return Persona.Nombre is { } nombre && Persona.Apellido is { } apellido
            && string.Equals(nombre, otra.Nombre, StringComparison.OrdinalIgnoreCase)
            && string.Equals(apellido, otra.Apellido, StringComparison.OrdinalIgnoreCase);
    }

    internal void AgregarTipo(EventoTipoEvento tipo) => _tipos.Add(tipo);

    /// <summary>Registra la atención o reemplaza la que tenía (p. ej. la ambulancia la completa al entregar al paciente).</summary>
    internal void RegistrarAtencion(AtencionPrehospitalaria atencion)
    {
        ArgumentNullException.ThrowIfNull(atencion);
        Atencion = atencion;
    }

    private void AsignarDatos(RolCiudadanoEnum rol, DatosPersona persona, EventoVehiculoInfo? vehiculo, Ciudadano? ciudadano)
    {
        if (!Enum.IsDefined(rol)) throw new DomainException("El rol del ciudadano no es válido.");
        if (vehiculo is null && RequiereVehiculo(rol))
            throw new DomainException($"El {Descripcion(rol)} debe estar asociado a un vehículo del evento.");
        if (vehiculo is not null && !AdmiteVehiculo(rol))
            throw new DomainException("Un peatón no puede estar asociado a un vehículo.");

        Rol = rol;
        // Copia propia: cada participación debe tener su instancia (el mismo DatosPersona
        // podría reutilizarse para varias personas y la persistencia los asocia por referencia)
        Persona = persona with { };
        Vehiculo = vehiculo;
        EventoVehiculoId = vehiculo is { Id: > 0 } ? vehiculo.Id : null;
        // Si cambia el ciudadano, la navegación cargada deja de corresponder
        Ciudadano = ciudadano;
        CiudadanoId = ciudadano is { Id: > 0 } ? ciudadano.Id : null;
    }

    private static string Descripcion(RolCiudadanoEnum rol) => rol == RolCiudadanoEnum.Conductor ? "conductor" : "pasajero";
}
