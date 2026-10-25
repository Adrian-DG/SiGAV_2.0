using Application.Common;
using Application.Contracts;
using Application.Contracts.Almacenamiento;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

public record ReporteEventoResult(byte[] Contenido, string NombreArchivo);

/// <summary>
/// Formulario de asistencia (APVI-FOR-003) del evento en PDF. Mismo acceso que el detalle: la app
/// solo lo obtiene de eventos de su unidad.
/// </summary>
public record GetReporteEventoQuery(int EventoId) : IRequest<ReporteEventoResult>;

public class GetReporteEventoQueryHandler(
    ISender sender,
    IReadDbContext db,
    IAlmacenamientoArchivos almacenamiento,
    IReporteAsistencia reporte) : IRequestHandler<GetReporteEventoQuery, ReporteEventoResult>
{
    private static readonly Dictionary<CanalReporteEnum, string> CanalTexto = new()
    {
        [CanalReporteEnum.CallCenter] = "Call center",
        [CanalReporteEnum.AgenteCampo] = "Asistencia en campo",
        [CanalReporteEnum.Agencia911] = "Sistema 9-1-1",
        [CanalReporteEnum.Whatsapp] = "WhatsApp",
        [CanalReporteEnum.CallCenter511] = "Call center *511",
    };

    private static readonly Dictionary<RolCiudadanoEnum, string> RolTexto = new()
    {
        [RolCiudadanoEnum.Conductor] = "Conductor",
        [RolCiudadanoEnum.Pasajero] = "Pasajero",
        [RolCiudadanoEnum.Peaton] = "Peatón",
        [RolCiudadanoEnum.Paciente] = "Paciente",
        [RolCiudadanoEnum.Otro] = "Otro",
    };

    public async Task<ReporteEventoResult> Handle(GetReporteEventoQuery request, CancellationToken cancellationToken)
    {
        // El detalle ya aplica el alcance de la sesión y lanza NotFound si no puede verlo
        var evento = await sender.Send(new GetEventoQuery(request.EventoId), cancellationToken);

        var persona = evento.Ciudadanos.FirstOrDefault(c => c.Rol == RolCiudadanoEnum.Conductor) ?? evento.Ciudadanos.FirstOrDefault();
        var vehiculo = evento.Vehiculos.FirstOrDefault(v => v.Id == persona?.VehiculoId) ?? evento.Vehiculos.FirstOrDefault();
        var unidad = evento.Unidades.FirstOrDefault(u => u.Rol == RolUnidadEventoEnum.Principal) ?? evento.Unidades.FirstOrDefault();

        string NombresTipos(IReadOnlyCollection<int> ids) =>
            string.Join(", ", evento.Tipos.Where(t => ids.Contains(t.Id)).Select(t => t.Nombre));

        // Lo atendido al vehículo; si no tiene (o no hay vehículo), lo de la persona o el evento completo
        var motivo = new[]
        {
            vehiculo is null ? string.Empty : NombresTipos(vehiculo.TipoEventoIds),
            persona is null ? string.Empty : NombresTipos(persona.TipoEventoIds),
            string.Join(", ", evento.Tipos.Select(t => t.Nombre)),
        }.FirstOrDefault(m => m.Length > 0);

        var otros = evento.Vehiculos
            .Where(v => v.Id != vehiculo?.Id)
            .Select(v => $"Vehículo: {v.Descripcion}{Sufijo(NombresTipos(v.TipoEventoIds))}")
            .Concat(evento.Ciudadanos
                .Where(c => c.Id != persona?.Id)
                .Select(c => $"{RolTexto.GetValueOrDefault(c.Rol, "Persona")}: {NombreCompleto(c.Nombre, c.Apellido) ?? "Sin nombre"}"
                    + (string.IsNullOrWhiteSpace(c.Identificacion) ? string.Empty : $" · {c.Identificacion}")
                    + Sufijo(NombresTipos(c.TipoEventoIds))))
            .ToList();

        var datos = new ReporteAsistenciaDatos(
            NumeroAsistencia: evento.Id,
            Anulado: !evento.IsActive,
            FechaHoraReporte: Local(evento.FechaHoraReporte),
            Nombre: persona?.Nombre,
            Apellido: persona?.Apellido,
            Identificacion: persona?.Identificacion,
            CantidadPersonas: evento.Ciudadanos.Count,
            Telefono: persona?.Telefono,
            UbicacionAsistencia: evento.Direccion,
            Provincia: evento.Provincia,
            Municipio: evento.Municipio,
            Tramo: evento.Tramo,
            Marca: vehiculo?.Marca,
            Modelo: vehiculo?.Modelo,
            TipoVehiculo: vehiculo?.TipoVehiculo,
            Color: vehiculo?.Color,
            Placa: vehiculo?.Placa,
            MotivoAsistencia: motivo,
            Denominacion: unidad?.Denominacion,
            FichaUnidad: unidad?.Ficha,
            OrdenadaPor: unidad?.Agente,
            ReportadoPor: CanalTexto.GetValueOrDefault(evento.CanalReporte),
            HoraDespacho: Local(evento.FechaHoraReporte),
            HoraLlegada: evento.FechaHoraLlegada is { } llegada ? Local(llegada) : null,
            HoraRetirada: evento.FechaHoraCompletado is { } completado ? Local(completado) : null,
            TipoCierre: evento.TipoCierre,
            Usuario: await UsuarioQueRegistroAsync(evento.Id, unidad, cancellationToken),
            Observaciones: evento.Comentario,
            OtrosInvolucrados: otros,
            FirmaCiudadano: await FirmaAsync(evento.Id, TipoEvidenciaEnum.FirmaCiudadano, persona?.Id, cancellationToken),
            FirmaAgente: await FirmaAsync(evento.Id, TipoEvidenciaEnum.FirmaAgente, null, cancellationToken));

        return new ReporteEventoResult(reporte.Generar(datos), $"asistencia-{evento.Id}.pdf");
    }

    private static DateTime Local(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaHorariaOperativa.Zona);

    private static string Sufijo(string tipos) => tipos.Length > 0 ? $" — {tipos}" : string.Empty;

    private static string? NombreCompleto(string? nombre, string? apellido)
        => $"{nombre} {apellido}".Trim() is { Length: > 0 } completo ? completo : null;

    /// <summary>
    /// Usuario web que lo registró (call center). Los registrados desde la app no tienen usuario
    /// web (CreatedBy = 0): los registró el agente de la unidad.
    /// </summary>
    private async Task<string?> UsuarioQueRegistroAsync(int eventoId, EventoUnidadViewModel? unidad, CancellationToken cancellationToken)
    {
        var creadoPor = await db.Eventos.Where(e => e.Id == eventoId).Select(e => e.CreatedBy).FirstAsync(cancellationToken);
        if (creadoPor > 0)
            return await db.Usuarios.Where(u => u.Id == creadoPor).Select(u => u.UserName).FirstOrDefaultAsync(cancellationToken);
        return unidad is null ? null : $"{unidad.Agente} (app móvil)";
    }

    /// <summary>La firma más reciente de ese tipo (la de la persona indicada, si la hay). Sin archivo: null.</summary>
    private async Task<byte[]?> FirmaAsync(int eventoId, TipoEvidenciaEnum tipo, int? ciudadanoId, CancellationToken cancellationToken)
    {
        var claves = await db.Eventos
            .Where(e => e.Id == eventoId)
            .SelectMany(e => e.Evidencias)
            .Where(ev => ev.Tipo == tipo)
            .OrderByDescending(ev => ev.Id)
            .Select(ev => new { ev.Ubicacion, ev.EventoCiudadanoId })
            .ToListAsync(cancellationToken);

        var clave = (claves.FirstOrDefault(c => ciudadanoId is not null && c.EventoCiudadanoId == ciudadanoId) ?? claves.FirstOrDefault())?.Ubicacion;
        if (clave is null) return null;

        var archivo = await almacenamiento.AbrirAsync(clave, cancellationToken);
        if (archivo is null) return null;

        await using var contenido = archivo.Contenido;
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancellationToken);
        return memoria.ToArray();
    }
}
