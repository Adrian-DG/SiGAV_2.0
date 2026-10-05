using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Última posición conocida de una unidad, enviada por la app móvil del agente que la opera.
/// Una sola fila por unidad (se actualiza en cada envío); no es un historial de recorridos.
/// La denominación no se copia: se consulta en la unidad, que puede ser reasignada en cualquier momento.
/// </summary>
public class UnidadPosicion
{
    public static readonly TimeSpan UmbralSinSenal = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan UmbralDesconectada = TimeSpan.FromMinutes(15);

    /// <summary>Envíos más seguidos que esto se ignoran (la app envía cada ~30 s).</summary>
    public static readonly TimeSpan IntervaloMinimo = TimeSpan.FromSeconds(10);

    /// <summary>Tolerancia ante relojes de dispositivos adelantados.</summary>
    public static readonly TimeSpan ToleranciaReloj = TimeSpan.FromMinutes(5);

    public int UnidadId { get; private set; }
    public virtual Unidad Unidad { get; private set; } = null!;

    /// <summary>Agente que envió la última posición.</summary>
    public int AgenteId { get; private set; }
    public virtual Agente Agente { get; private set; } = null!;

    public Coordenada Ubicacion { get; private set; } = null!;

    /// <summary>Radio de incertidumbre reportado por el GPS, en metros.</summary>
    public double? PrecisionMetros { get; private set; }

    /// <summary>Dirección de avance en grados (0 = norte, sentido horario).</summary>
    public double? Rumbo { get; private set; }

    public double? VelocidadKmh { get; private set; }

    /// <summary>Momento de la lectura según el dispositivo (UTC).</summary>
    public DateTime FechaHoraGpsUtc { get; private set; }

    /// <summary>Momento en que la recibió el servidor (UTC): define el estado de la conexión.</summary>
    public DateTime FechaHoraRecibidaUtc { get; private set; }

    /// <summary>False cuando el agente cerró sesión (o front desk la cerró): la unidad sale como desconectada.</summary>
    public bool SesionActiva { get; private set; }

    /// <summary>
    /// Cuándo se cerró la sesión por última vez. Los tokens emitidos antes ya no pueden enviar
    /// posiciones (un envío atrasado de la app no reabre una sesión que se cerró).
    /// </summary>
    public DateTime? SesionCerradaUtc { get; private set; }

    // Requerido por EF Core
    private UnidadPosicion() { }

    public static UnidadPosicion Crear(int unidadId, int agenteId, LecturaGps lectura, DateTime ahoraUtc)
    {
        var posicion = new UnidadPosicion { UnidadId = unidadId };
        posicion.Aplicar(agenteId, lectura, ahoraUtc);
        return posicion;
    }

    /// <summary>
    /// Registra una lectura nueva. Devuelve false (sin cambios) si es más antigua que la guardada
    /// (llegó tarde o desordenada) o si llega antes de <see cref="IntervaloMinimo"/> del mismo agente.
    /// </summary>
    public bool Registrar(int agenteId, LecturaGps lectura, DateTime ahoraUtc)
    {
        Validar(lectura, ahoraUtc);
        if (OcupadaPorOtroAgente(agenteId, ahoraUtc))
            throw new DomainException("La unidad está siendo operada por otro agente.");

        var mismoAgente = AgenteId == agenteId && SesionActiva;
        if (mismoAgente && lectura.FechaHoraUtc <= FechaHoraGpsUtc) return false;
        if (mismoAgente && ahoraUtc - FechaHoraRecibidaUtc < IntervaloMinimo) return false;

        Aplicar(agenteId, lectura, ahoraUtc);
        return true;
    }

    public void CerrarSesion(DateTime ahoraUtc)
    {
        SesionActiva = false;
        SesionCerradaUtc = ahoraUtc;
    }

    /// <summary>
    /// El token se emitió antes del último cierre de sesión, así que pertenece a la sesión cerrada.
    /// El iat del JWT tiene precisión de segundos: un token emitido en el mismo segundo del cierre
    /// también cuenta como cerrado (volver a iniciar sesión toma bastante más de un segundo).
    /// </summary>
    public bool SesionCerradaParaToken(DateTime tokenEmitidoUtc)
        => SesionCerradaUtc is { } cerrada && tokenEmitidoUtc <= cerrada;

    /// <summary>
    /// Otro agente tiene una sesión abierta en la unidad y sigue enviando su posición. Pasado
    /// <see cref="UmbralDesconectada"/> sin envíos se considera abandonada y otro agente puede tomarla.
    /// </summary>
    public bool OcupadaPorOtroAgente(int agenteId, DateTime ahoraUtc)
        => AgenteId != agenteId && Estado(ahoraUtc) != EstadoPosicionEnum.Desconectada;

    public EstadoPosicionEnum Estado(DateTime ahoraUtc) => CalcularEstado(SesionActiva, FechaHoraRecibidaUtc, ahoraUtc);

    /// <summary>Misma regla que <see cref="Estado"/>, para proyecciones de consultas.</summary>
    public static EstadoPosicionEnum CalcularEstado(bool sesionActiva, DateTime recibidaUtc, DateTime ahoraUtc)
    {
        var transcurrido = ahoraUtc - recibidaUtc;
        if (!sesionActiva || transcurrido >= UmbralDesconectada) return EstadoPosicionEnum.Desconectada;
        return transcurrido >= UmbralSinSenal ? EstadoPosicionEnum.SinSenal : EstadoPosicionEnum.EnLinea;
    }

    private static void Validar(LecturaGps lectura, DateTime ahoraUtc)
    {
        if (lectura.FechaHoraUtc == default) throw new DomainException("La fecha de la lectura GPS es requerida.");
        if (lectura.FechaHoraUtc > ahoraUtc + ToleranciaReloj) throw new DomainException("La fecha de la lectura GPS no puede estar en el futuro.");
    }

    private void Aplicar(int agenteId, LecturaGps lectura, DateTime ahoraUtc)
    {
        Validar(lectura, ahoraUtc);
        AgenteId = agenteId;
        Ubicacion = lectura.Ubicacion;
        PrecisionMetros = lectura.PrecisionMetros;
        Rumbo = lectura.Rumbo;
        VelocidadKmh = lectura.VelocidadKmh;
        FechaHoraGpsUtc = lectura.FechaHoraUtc;
        FechaHoraRecibidaUtc = ahoraUtc;
        SesionActiva = true;
    }
}

/// <summary>Una lectura del GPS del dispositivo.</summary>
public sealed record LecturaGps(Coordenada Ubicacion, double? PrecisionMetros, double? Rumbo, double? VelocidadKmh, DateTime FechaHoraUtc);
