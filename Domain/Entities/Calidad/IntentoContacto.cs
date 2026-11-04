using Domain.Abstraction;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Calidad;

/// <summary>
/// Un intento de contactar al ciudadano para la evaluación de calidad (llamada o mensaje). Registro
/// de auditoría: inmutable. Lo crea únicamente <see cref="EvaluacionCalidad.RegistrarIntento"/>.
/// </summary>
public class IntentoContacto : IRegistroInmutable
{
    public const int ObservacionMaxLength = 500;
    public const int ReferenciaExternaMaxLength = 100;

    public long Id { get; private set; }
    public int EvaluacionCalidadId { get; private set; }

    public DateTime FechaUtc { get; private set; }
    public CanalContactoEnum Canal { get; private set; }
    public ResultadoContactoEnum Resultado { get; private set; }

    /// <summary>Usuario web que lo registró. null = lo registró el bot.</summary>
    public int? UsuarioId { get; private set; }

    public string? Observacion { get; private set; }

    /// <summary>Id del mensaje en el proveedor de mensajería: evita registrar dos veces un reenvío del webhook.</summary>
    public string? ReferenciaExterna { get; private set; }

    // Requerido por EF Core
    private IntentoContacto() { }

    internal static IntentoContacto Crear(
        CanalContactoEnum canal,
        ResultadoContactoEnum resultado,
        int? usuarioId,
        string? observacion,
        DateTime fechaUtc,
        string? referenciaExterna)
    {
        if (!Enum.IsDefined(canal)) throw new DomainException("El canal de contacto no es válido.");
        if (!Enum.IsDefined(resultado)) throw new DomainException("El resultado del contacto no es válido.");
        if (canal == CanalContactoEnum.Llamada && resultado is ResultadoContactoEnum.MensajeEnviado or ResultadoContactoEnum.SinRespuesta)
            throw new DomainException("Ese resultado solo aplica a mensajes.");

        return new IntentoContacto
        {
            Canal = canal,
            Resultado = resultado,
            UsuarioId = usuarioId,
            FechaUtc = fechaUtc,
            Observacion = Limpiar(observacion, ObservacionMaxLength, "La observación"),
            ReferenciaExterna = Limpiar(referenciaExterna, ReferenciaExternaMaxLength, "La referencia externa")
        };
    }

    private static string? Limpiar(string? valor, int maxLength, string campo)
    {
        var limpio = valor?.Trim();
        if (string.IsNullOrEmpty(limpio)) return null;
        if (limpio.Length > maxLength) throw new DomainException($"{campo} no puede exceder {maxLength} caracteres.");
        return limpio;
    }
}
