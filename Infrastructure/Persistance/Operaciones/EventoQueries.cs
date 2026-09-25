using Application.Common.Models;
using Application.Contracts.Operaciones;
using Application.Features.Operaciones.Eventos;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Operaciones;

public class EventoQueries(SiGAVContext context) : IEventoQueries
{
    public async Task<PagedResult<EventoListItemViewModel>> GetPagedAsync(FiltroEventos filtro, int page, int size, CancellationToken cancellationToken = default)
    {
        var query = context.Eventos.AsNoTracking();

        if (!filtro.IncluirAnulados) query = query.Where(e => e.IsActive);
        if (filtro.Estado is { } estado) query = query.Where(e => e.Estado == estado);
        if (filtro.DesdeUtc is { } desde) query = query.Where(e => e.FechaHoraReporteUtc >= desde);
        if (filtro.HastaExclusivoUtc is { } hasta) query = query.Where(e => e.FechaHoraReporteUtc < hasta);
        if (filtro.UnidadId is { } unidadId) query = query.Where(e => e.Unidades.Any(u => u.UnidadId == unidadId));

        var total = await query.CountAsync(cancellationToken);

        var filas = await query
            .OrderByDescending(e => e.FechaHoraReporteUtc)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new
            {
                e.Id,
                e.Estado,
                e.Direccion,
                e.FechaHoraReporteUtc,
                Tipos = e.Tipos.Select(t => new { t.TipoEvento!.Nombre, t.TipoEvento.Categoria }).ToList(),
                Principal = e.Unidades
                    .Where(u => u.Rol == RolUnidadEventoEnum.Principal)
                    .Select(u => new { u.Unidad!.Ficha, Denominacion = u.Denominacion!.Nombre })
                    .FirstOrDefault(),
                Persona = e.Ciudadanos.OrderBy(c => c.Id).Select(c => c.Persona).FirstOrDefault(),
                Vehiculo = e.Ciudadanos.Where(c => c.Vehiculo != null).OrderBy(c => c.Id).Select(c => c.Vehiculo).FirstOrDefault()
            })
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var catalogo = await CargarCatalogosVehiculoAsync(filas.Select(f => f.Vehiculo), cancellationToken);

        var items = filas.Select(f => new EventoListItemViewModel(
                f.Id,
                f.Estado,
                f.Tipos.Select(t => t.Nombre).OrderBy(n => n).ToList(),
                f.Tipos.Select(t => t.Categoria).Distinct().Order().ToList(),
                NombrePersona(f.Persona),
                f.Vehiculo is null ? null : catalogo.Describir(f.Vehiculo).Descripcion,
                f.Direccion,
                f.FechaHoraReporteUtc,
                f.Principal?.Ficha ?? string.Empty,
                f.Principal?.Denominacion ?? string.Empty))
            .ToList();

        return new PagedResult<EventoListItemViewModel>(items, page, size, total);
    }

    public async Task<EventoDetalleViewModel?> GetDetalleAsync(int eventoId, int? unidadId, CancellationToken cancellationToken = default)
    {
        var query = context.Eventos.AsNoTracking().Where(e => e.Id == eventoId);
        if (unidadId is { } id) query = query.Where(e => e.Unidades.Any(u => u.UnidadId == id));

        var evento = await query
            .Include(e => e.Municipio!).ThenInclude(m => m.Provincia)
            .Include(e => e.Tramo)
            .Include(e => e.Tipos).ThenInclude(t => t.TipoEvento)
            .Include(e => e.Unidades).ThenInclude(u => u.Unidad)
            .Include(e => e.Unidades).ThenInclude(u => u.Denominacion)
            .Include(e => e.Unidades).ThenInclude(u => u.NivelDenominacion)
            .Include(e => e.Unidades).ThenInclude(u => u.Agente!).ThenInclude(a => a.Rango)
            .Include(e => e.Ciudadanos)
            .Include(e => e.Evidencias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (evento is null) return null;

        var catalogo = await CargarCatalogosVehiculoAsync(evento.Ciudadanos.Select(c => c.Vehiculo), cancellationToken);
        var nacionalidadIds = evento.Ciudadanos.Select(c => c.Persona.NacionalidadId).OfType<int>().Distinct().ToList();
        var nacionalidades = nacionalidadIds.Count == 0
            ? new Dictionary<int, string>()
            : await context.Nacionalidades.AsNoTracking()
                .Where(n => nacionalidadIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Nombre, cancellationToken);

        return new EventoDetalleViewModel(
            evento.Id,
            evento.RequestId,
            evento.Estado,
            evento.CanalReporte,
            evento.TipoCierre,
            evento.IsActive,
            evento.Ubicacion.Latitud,
            evento.Ubicacion.Longitud,
            evento.Direccion,
            evento.MunicipioId,
            evento.Municipio?.Nombre ?? string.Empty,
            evento.Municipio?.Provincia?.Nombre ?? string.Empty,
            evento.TramoId,
            evento.Tramo?.Nombre,
            evento.Comentario,
            evento.FechaHoraReporteUtc,
            evento.FechaHoraLlegadaUtc,
            evento.FechaHoraCompletadoUtc,
            evento.Tipos
                .Select(t => new EventoTipoViewModel(t.TipoEventoId, t.TipoEvento?.Nombre ?? string.Empty, t.TipoEvento?.Categoria ?? default))
                .OrderBy(t => t.Nombre)
                .ToList(),
            evento.Unidades
                .OrderBy(u => u.Rol)
                .Select(u => new EventoUnidadViewModel(
                    u.UnidadId,
                    u.Unidad?.Ficha ?? string.Empty,
                    u.DenominacionId,
                    u.Denominacion?.Nombre ?? string.Empty,
                    u.NivelDenominacion?.Nombre ?? string.Empty,
                    u.Rol,
                    u.AgenteId,
                    u.Agente?.GetInfo ?? string.Empty))
                .ToList(),
            evento.Ciudadanos
                .OrderBy(c => c.Id)
                .Select(c => new EventoCiudadanoViewModel(
                    c.Id,
                    c.Rol,
                    c.Persona.Identificacion,
                    c.Persona.Nombre,
                    c.Persona.Apellido,
                    c.Persona.Sexo,
                    c.Persona.Telefono,
                    c.Persona.NacionalidadId is { } n ? nacionalidades.GetValueOrDefault(n) : null,
                    c.Vehiculo is null ? null : catalogo.Describir(c.Vehiculo)))
                .ToList(),
            evento.Evidencias
                .OrderBy(e => e.Id)
                .Select(e => new EventoEvidenciaViewModel(e.Id, e.Tipo, e.Ubicacion, e.ContentType, e.RegistradaUtc))
                .ToList(),
            evento.CreatedAt);
    }

    private static string? NombrePersona(DatosPersona? persona)
    {
        if (persona is null) return null;
        var nombre = $"{persona.Nombre} {persona.Apellido}".Trim();
        return nombre.Length > 0 ? nombre : persona.Identificacion ?? "Persona no identificada";
    }

    /// <summary>Nombres de tipo, marca, modelo y color de los vehículos indicados, en un solo viaje por catálogo.</summary>
    private async Task<CatalogoVehiculo> CargarCatalogosVehiculoAsync(IEnumerable<DatosVehiculo?> vehiculos, CancellationToken cancellationToken)
    {
        var lista = vehiculos.OfType<DatosVehiculo>().ToList();
        var tipoIds = lista.Select(v => v.TipoVehiculoId).OfType<int>().Distinct().ToList();
        var marcaIds = lista.Select(v => v.MarcaId).OfType<int>().Distinct().ToList();
        var modeloIds = lista.Select(v => v.ModeloId).OfType<int>().Distinct().ToList();
        var colorIds = lista.Select(v => v.ColorId).OfType<int>().Distinct().ToList();

        return new CatalogoVehiculo(
            tipoIds.Count == 0 ? [] : await context.TipoVehiculos.AsNoTracking().Where(x => tipoIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken),
            marcaIds.Count == 0 ? [] : await context.Marcas.AsNoTracking().Where(x => marcaIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken),
            modeloIds.Count == 0 ? [] : await context.Modelos.AsNoTracking().Where(x => modeloIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken),
            colorIds.Count == 0 ? [] : await context.Colores.AsNoTracking().Where(x => colorIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Nombre, cancellationToken));
    }

    private sealed record CatalogoVehiculo(
        Dictionary<int, string> Tipos,
        Dictionary<int, string> Marcas,
        Dictionary<int, string> Modelos,
        Dictionary<int, string> Colores)
    {
        /// <summary>Nombre de catálogo o, si el dato no estaba en el catálogo, el texto libre.</summary>
        public EventoVehiculoViewModel Describir(DatosVehiculo v)
        {
            var tipo = v.TipoVehiculoId is { } t ? Tipos.GetValueOrDefault(t) : null;
            var marca = v.MarcaId is { } m ? Marcas.GetValueOrDefault(m) : v.MarcaTexto;
            var modelo = v.ModeloId is { } mo ? Modelos.GetValueOrDefault(mo) : v.ModeloTexto;
            var color = v.ColorId is { } c ? Colores.GetValueOrDefault(c) : v.ColorTexto;

            var marcaModelo = string.Join(' ', new[] { marca, modelo }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var partes = new[] { marcaModelo.Length > 0 ? marcaModelo : tipo, color, v.Placa ?? "Sin placa" }
                .Where(s => !string.IsNullOrWhiteSpace(s));

            return new EventoVehiculoViewModel(v.Placa, tipo, marca, modelo, color, string.Join(" · ", partes));
        }
    }
}
