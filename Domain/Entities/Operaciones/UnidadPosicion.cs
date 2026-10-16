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

    /// <summary>Segundo agente (componente) de la sesión que envió la última posición, si lo hay.</summary>
    public int? ComponenteId { get; private set; }
    public virtual Agente? Componente { get; private set; }

    /// <summary>
    /// El agente de la sesión atiende un evento que la app aún tiene en el dispositivo (en curso, sin
    /// enviar): el servidor no lo conoce hasta que se cierra y se envía, así que lo reporta la app.
    /// </summary>
    public bool AgenteOcupadoEnDispositivo { get; private set; }

    /// <summary>Igual que <see cref="AgenteOcupadoEnDispositivo"/>, para el componente.</summary>
    public bool ComponenteOcupadoEnDispositivo { get; private set; }

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

    /// <summary><paramref name="ocupacion"/>: la que reportó la app; null (una versión que no la envía) = libres.</summary>
    public static UnidadPosicion Crear(int unidadId, int agenteId, int? componenteId, LecturaGps lectura, OcupacionAgentes? ocupacion, DateTime ahoraUtc)
    {
        var posicion = new UnidadPosicion { UnidadId = unidadId };
        posicion.Aplicar(agenteId, componenteId, lectura, ocupacion ?? OcupacionAgentes.Libres, ahoraUtc);
        return posicion;
    }

    /// <summary>
    /// Registra una lectura nueva. Si es más antigua que la guardada (llegó tarde o desordenada) o
    /// llega antes de <see cref="IntervaloMinimo"/> del mismo agente, solo se toma la ocupación (es el
    /// estado actual de la app, sin importar la lectura). Devuelve false si no cambió nada.
    /// <paramref name="ocupacion"/> null (una versión de la app que no la envía): se conserva la de la
    /// misma tripulación; con otra tripulación, libres.
    /// </summary>
    public bool Registrar(int agenteId, int? componenteId, LecturaGps lectura, OcupacionAgentes? ocupacion, DateTime ahoraUtc)
    {
        Validar(lectura, ahoraUtc);
        if (OcupadaPorOtroAgente(agenteId, ahoraUtc))
            throw new DomainException("La unidad está siendo operada por otro agente.");

        var mismoAgente = AgenteId == agenteId && ComponenteId == componenteId && SesionActiva;
        if (mismoAgente && (lectura.FechaHoraUtc <= FechaHoraGpsUtc || ahoraUtc - FechaHoraRecibidaUtc < IntervaloMinimo))
            return ocupacion is { } nueva && CambiarOcupacion(nueva);

        Aplicar(agenteId, componenteId, lectura, ocupacion ?? (mismoAgente ? Ocupacion : OcupacionAgentes.Libres), ahoraUtc);
        return true;
    }

    public OcupacionAgentes Ocupacion => new(AgenteOcupadoEnDispositivo, ComponenteOcupadoEnDispositivo);

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

    private bool CambiarOcupacion(OcupacionAgentes ocupacion)
    {
        // Sin componente no hay a quién marcar
        var nueva = ocupacion with { Componente = ComponenteId is not null && ocupacion.Componente };
        if (nueva == Ocupacion) return false;

        AgenteOcupadoEnDispositivo = nueva.Agente;
        ComponenteOcupadoEnDispositivo = nueva.Componente;
        return true;
    }

    private void Aplicar(int agenteId, int? componenteId, LecturaGps lectura, OcupacionAgentes ocupacion, DateTime ahoraUtc)
    {
        Validar(lectura, ahoraUtc);
        AgenteId = agenteId;
        if (Componente?.Id != componenteId) Componente = null;
        ComponenteId = componenteId;
        Ubicacion = lectura.Ubicacion;
        PrecisionMetros = lectura.PrecisionMetros;
        Rumbo = lectura.Rumbo;
        VelocidadKmh = lectura.VelocidadKmh;
        FechaHoraGpsUtc = lectura.FechaHoraUtc;
        FechaHoraRecibidaUtc = ahoraUtc;
        SesionActiva = true;
        CambiarOcupacion(ocupacion);
    }
}

/// <summary>
/// Qué agentes de la unidad atienden un evento que la app tiene en el dispositivo (en curso, aún sin
/// enviar). <see cref="Componente"/> solo aplica si la sesión tiene componente.
/// </summary>
public readonly record struct OcupacionAgentes(bool Agente, bool Componente)
{
    public static readonly OcupacionAgentes Libres = new(false, false);
}

/// <summary>Una lectura del GPS del dispositivo.</summary>
public sealed record LecturaGps(Coordenada Ubicacion, double? PrecisionMetros, double? Rumbo, double? VelocidadKmh, DateTime FechaHoraUtc);
