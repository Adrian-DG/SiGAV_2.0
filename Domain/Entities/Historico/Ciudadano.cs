using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Historico;

/// <summary>
/// Persona conocida, por cédula o pasaporte. Se crea al registrar un evento con una identificación
/// que aún no existe (o viene importada de SiGAV 1.0) y cada evento posterior la actualiza: lo más
/// reciente gana. El evento guarda su propia foto de los datos, así que corregir el maestro no
/// altera reportes.
/// </summary>
public class Ciudadano : BaseEntityMetadata
{
    public required string Identificacion { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public SexoEnum Sexo { get; set; }
    public string? Telefono { get; set; }

    public int? NacionalidadId { get; set; }
    public virtual Nacionalidad? Nacionalidad { get; set; }

    /// <summary>Calculada con la edad declarada o estimada en el evento: solo sirve para estimar la edad actual.</summary>
    public DateOnly? FechaNacimientoEstimada { get; set; }

    /// <summary>Fecha de reporte del evento más reciente que actualizó los datos. null = solo importado.</summary>
    public DateTime? UltimoEventoUtc { get; set; }

    /// <summary>Persona nueva a partir de un evento. Requiere identificación (el resto es opcional).</summary>
    public static Ciudadano Registrar(DatosPersona persona, DateTime fechaEventoUtc)
    {
        ArgumentNullException.ThrowIfNull(persona);
        var ciudadano = new Ciudadano
        {
            Identificacion = persona.Identificacion
                ?? throw new DomainException("Solo se guarda en el histórico una persona identificada."),
            IsActive = true
        };
        ciudadano.Actualizar(persona, fechaEventoUtc);
        return ciudadano;
    }

    /// <summary>
    /// Toma los datos de un evento. Si el evento es más antiguo que el último aplicado (p. ej. se
    /// sincronizó tarde desde la cola offline) solo completa lo que falta, sin pisar datos nuevos.
    /// </summary>
    public void Actualizar(DatosPersona persona, DateTime fechaEventoUtc)
    {
        ArgumentNullException.ThrowIfNull(persona);
        if (persona.Identificacion != Identificacion)
            throw new DomainException("Los datos no corresponden a la persona del histórico.");

        var masReciente = UltimoEventoUtc is null || fechaEventoUtc >= UltimoEventoUtc;

        Nombre = Elegir(Nombre, persona.Nombre, masReciente);
        Apellido = Elegir(Apellido, persona.Apellido, masReciente);
        Telefono = Elegir(Telefono, persona.Telefono, masReciente);
        if (persona.NacionalidadId is { } nacionalidadId && (masReciente || NacionalidadId is null))
        {
            // Si cambia la nacionalidad, la navegación cargada deja de corresponder
            if (Nacionalidad?.Id != nacionalidadId) Nacionalidad = null;
            NacionalidadId = nacionalidadId;
        }
        if (persona.Sexo != SexoEnum.NONE && (masReciente || Sexo == SexoEnum.NONE))
            Sexo = persona.Sexo;
        if (persona.Edad is { } edad && (masReciente || FechaNacimientoEstimada is null))
            FechaNacimientoEstimada = DateOnly.FromDateTime(fechaEventoUtc).AddYears(-edad);

        if (masReciente) UltimoEventoUtc = fechaEventoUtc;
    }

    /// <summary>Edad cumplida a <paramref name="fechaUtc"/> según la fecha de nacimiento estimada.</summary>
    public static int? EdadAl(DateOnly? fechaNacimiento, DateTime fechaUtc)
    {
        if (fechaNacimiento is not { } nacimiento) return null;

        var hoy = DateOnly.FromDateTime(fechaUtc);
        var edad = hoy.Year - nacimiento.Year;
        if (hoy < nacimiento.AddYears(edad)) edad--;
        return Math.Clamp(edad, 0, DatosPersona.EdadMaxima);
    }

    private static string? Elegir(string? actual, string? nuevo, bool masReciente)
        => nuevo is not null && (masReciente || actual is null) ? nuevo : actual;
}
