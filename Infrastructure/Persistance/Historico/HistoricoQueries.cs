using Application.Contracts.Historico;
using Application.Features.Historico;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Historico;

public class HistoricoQueries(SiGAVContext context) : IHistoricoQueries
{
    public async Task<CiudadanoConocidoViewModel?> BuscarCiudadanoAsync(string identificacionNormalizada, CancellationToken cancellationToken = default)
    {
        // Lo más reciente: el último evento activo en que se registró esa cédula
        var ultimo = await context.EventoCiudadanos.AsNoTracking()
            .Where(c => c.Persona.Identificacion == identificacionNormalizada && c.Evento!.IsActive)
            .OrderByDescending(c => c.Evento!.FechaHoraReporteUtc)
            .ThenByDescending(c => c.Id)
            .Select(c => new
            {
                c.Persona.Nombre,
                c.Persona.Apellido,
                c.Persona.Sexo,
                c.Persona.Telefono,
                c.Persona.NacionalidadId,
                c.Evento!.FechaHoraReporteUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ultimo is not null)
            return new CiudadanoConocidoViewModel(identificacionNormalizada, ultimo.Nombre, ultimo.Apellido, ultimo.Sexo,
                ultimo.Telefono, ultimo.NacionalidadId, OrigenDato.Evento, ultimo.FechaHoraReporteUtc);

        var maestro = await context.Ciudadanos.AsNoTracking()
            .Where(c => c.Identificacion == identificacionNormalizada && c.IsActive)
            .Select(c => new { c.Nombre, c.Apellido, c.Sexo, c.NacionalidadId })
            .FirstOrDefaultAsync(cancellationToken);

        return maestro is null
            ? null
            : new CiudadanoConocidoViewModel(identificacionNormalizada, maestro.Nombre, maestro.Apellido, maestro.Sexo,
                null, maestro.NacionalidadId, OrigenDato.Maestro, null);
    }

    public async Task<VehiculoConocidoViewModel?> BuscarVehiculoAsync(string placaNormalizada, CancellationToken cancellationToken = default)
    {
        var ultimo = await context.EventoCiudadanos.AsNoTracking()
            .Where(c => c.Vehiculo != null && c.Vehiculo.Placa == placaNormalizada && c.Evento!.IsActive)
            .OrderByDescending(c => c.Evento!.FechaHoraReporteUtc)
            .ThenByDescending(c => c.Id)
            .Select(c => new { c.Vehiculo, c.Evento!.FechaHoraReporteUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (ultimo?.Vehiculo is { } v)
            return new VehiculoConocidoViewModel(placaNormalizada, v.TipoVehiculoId, v.MarcaId, v.ModeloId, v.ColorId,
                v.MarcaTexto, v.ModeloTexto, v.ColorTexto, OrigenDato.Evento, ultimo.FechaHoraReporteUtc);

        var maestro = await context.Vehiculos.AsNoTracking()
            .Where(x => x.Placa == placaNormalizada && x.IsActive)
            .Select(x => new { x.TipoId, x.ColorId, x.ModeloId, MarcaId = x.Modelo != null ? (int?)x.Modelo.MarcaId : null })
            .FirstOrDefaultAsync(cancellationToken);

        return maestro is null
            ? null
            : new VehiculoConocidoViewModel(placaNormalizada, maestro.TipoId, maestro.MarcaId, maestro.ModeloId, maestro.ColorId,
                null, null, null, OrigenDato.Maestro, null);
    }
}
