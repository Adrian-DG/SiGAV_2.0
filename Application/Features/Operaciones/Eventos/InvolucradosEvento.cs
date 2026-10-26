using Application.Contracts;
using Domain.Entities.Historico;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Vehículos y personas de un request agregados al evento (al registrarlo o al completarlo en campo).
/// Cada persona identificada y cada vehículo con placa se guarda en el histórico si no existe, o lo
/// actualiza si ya existe (lo más reciente gana), para autocompletar en eventos futuros.
/// </summary>
internal static class InvolucradosEvento
{
    /// <summary>Agrega los vehículos y luego las personas (que se asocian a ellos por su clave), en el orden del request.</summary>
    public static async Task<(List<EventoVehiculoInfo> Vehiculos, List<EventoCiudadanoInfo> Ciudadanos)> AgregarAsync(
        Evento evento,
        IReadOnlyList<VehiculoEventoRequest>? vehiculos,
        IReadOnlyList<CiudadanoEventoRequest>? ciudadanos,
        IHistoricoRepository historico,
        CancellationToken cancellationToken)
    {
        var vehiculoPorClave = new Dictionary<string, EventoVehiculoInfo>(StringComparer.Ordinal);
        var vehiculosCreados = new List<EventoVehiculoInfo>();
        var ciudadanosCreados = new List<EventoCiudadanoInfo>();

        foreach (var v in vehiculos ?? [])
        {
            var datos = new DatosVehiculo(v.Placa, v.TipoVehiculoId, v.MarcaId, v.ModeloId, v.ColorId, v.MarcaTexto, v.ModeloTexto, v.ColorTexto);
            var vehiculoHistorico = await VehiculoHistoricoAsync(historico, datos, evento.FechaHoraReporteUtc, cancellationToken);
            vehiculoPorClave[v.Clave] = evento.AgregarVehiculo(datos, vehiculoHistorico, v.TipoEventoIds);
            vehiculosCreados.Add(vehiculoPorClave[v.Clave]);
        }

        foreach (var c in ciudadanos ?? [])
        {
            var persona = new DatosPersona(c.Identificacion, c.Nombre, c.Apellido, c.Sexo, c.Telefono, c.NacionalidadId, c.Edad);
            var ciudadano = await CiudadanoHistoricoAsync(historico, persona, evento.FechaHoraReporteUtc, cancellationToken);
            var vehiculo = c.VehiculoClave is { } clave ? vehiculoPorClave[clave] : null;

            ciudadanosCreados.Add(evento.AgregarCiudadano(c.Rol, persona, vehiculo, ciudadano, c.TipoEventoIds));
        }

        return (vehiculosCreados, ciudadanosCreados);
    }

    /// <summary>
    /// Reenvío: el evento ya tiene lo de aquel request. Los Ids se devuelven en el orden del request
    /// (la app asocia por posición las fotos de placa y cédula): el orden de los Ids no sirve, porque
    /// al guardar EF inserta antes a quien no depende de un vehículo nuevo. Se empareja por placa y por
    /// identificación o nombre; lo que no coincida, por orden.
    /// </summary>
    public static async Task<RegistrarEventoResult> ResultadoExistenteAsync(
        IReadDbContext db,
        int eventoId,
        IReadOnlyList<VehiculoEventoRequest>? vehiculos,
        IReadOnlyList<CiudadanoEventoRequest>? ciudadanos,
        CancellationToken cancellationToken)
    {
        var vehiculosGuardados = await db.EventoVehiculos
            .Where(v => v.EventoId == eventoId)
            .OrderBy(v => v.Id)
            .Select(v => new { v.Id, v.Datos.Placa })
            .ToListAsync(cancellationToken);
        var ciudadanosGuardados = await db.EventoCiudadanos
            .Where(c => c.EventoId == eventoId)
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Rol, c.Persona.Identificacion, c.Persona.Nombre, c.Persona.Apellido })
            .ToListAsync(cancellationToken);

        var vehiculoIds = Emparejar(
            vehiculos ?? [], vehiculosGuardados, v => v.Id,
            (pedido, guardado) => Clave(PlacaONull(pedido.Placa)) is { } placa && placa == Clave(guardado.Placa));
        var ciudadanoIds = Emparejar(
            ciudadanos ?? [], ciudadanosGuardados, c => c.Id,
            (pedido, guardado) => pedido.Rol == guardado.Rol && (Clave(pedido.Identificacion) is { } identificacion
                ? identificacion == Clave(guardado.Identificacion)
                : Clave(pedido.Nombre) == Clave(guardado.Nombre) && Clave(pedido.Apellido) == Clave(guardado.Apellido)));

        return new RegistrarEventoResult(eventoId, EsDuplicado: true, vehiculoIds, ciudadanoIds);
    }

    /// <summary>Para cada elemento del request, el Id del guardado que le corresponde (cada guardado se usa una vez).</summary>
    private static List<int> Emparejar<TPedido, TGuardado>(
        IReadOnlyList<TPedido> pedidos, IReadOnlyList<TGuardado> guardados, Func<TGuardado, int> id, Func<TPedido, TGuardado, bool> coincide)
    {
        var libres = guardados.ToList();
        var ids = new List<int>();
        foreach (var pedido in pedidos)
        {
            var guardado = libres.FirstOrDefault(g => coincide(pedido, g)) ?? libres.FirstOrDefault();
            if (guardado is null) break;
            ids.Add(id(guardado));
            libres.Remove(guardado);
        }
        return ids;
    }

    /// <summary>Texto comparable: solo letras y dígitos, en mayúsculas (null si queda vacío).</summary>
    private static string? Clave(string? valor)
        => new string((valor ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant() is { Length: > 0 } clave ? clave : null;

    private static string? PlacaONull(string? placa)
    {
        try { return DatosVehiculo.NormalizarPlaca(placa); }
        catch (Domain.Exceptions.DomainException) { return null; }
    }

    /// <summary>
    /// Persona del histórico para vincular a la participación: se crea si la identificación no existe
    /// y se actualiza si existe. Sin identificación no hay a quién guardar; uno desactivado no se toca
    /// ni se vincula.
    /// </summary>
    private static async Task<Ciudadano?> CiudadanoHistoricoAsync(
        IHistoricoRepository historico, DatosPersona persona, DateTime fechaEventoUtc, CancellationToken cancellationToken)
    {
        if (persona.Identificacion is not { } identificacion) return null;

        if (await historico.GetCiudadanoAsync(identificacion, cancellationToken) is not { } ciudadano)
        {
            ciudadano = Ciudadano.Registrar(persona, fechaEventoUtc);
            historico.Add(ciudadano);
            return ciudadano;
        }

        if (!ciudadano.IsActive) return null;
        ciudadano.Actualizar(persona, fechaEventoUtc);
        return ciudadano;
    }

    /// <summary>Igual que <see cref="CiudadanoHistoricoAsync"/>, por placa.</summary>
    private static async Task<Vehiculo?> VehiculoHistoricoAsync(
        IHistoricoRepository historico, DatosVehiculo datos, DateTime fechaEventoUtc, CancellationToken cancellationToken)
    {
        if (datos.Placa is not { } placa) return null;

        if (await historico.GetVehiculoAsync(placa, cancellationToken) is not { } vehiculo)
        {
            vehiculo = Vehiculo.Registrar(datos, fechaEventoUtc);
            historico.Add(vehiculo);
            return vehiculo;
        }

        if (!vehiculo.IsActive) return null;
        vehiculo.Actualizar(datos, fechaEventoUtc);
        return vehiculo;
    }
}
