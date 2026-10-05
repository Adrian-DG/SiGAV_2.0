using Application.Contracts.Almacenamiento;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Almacenamiento;

public static class AlmacenamientoExtensions
{
    /// <summary>
    /// Registra el proveedor de "Almacenamiento:Proveedor". Para agregar Azure Blob Storage o
    /// Cloudinary: implementar IAlmacenamientoArchivos, sumar sus opciones a AlmacenamientoOptions
    /// y un caso aquí. Nada más cambia (la base guarda claves, no URLs).
    /// </summary>
    public static IServiceCollection AddAlmacenamiento(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var opciones = configuration.GetSection(AlmacenamientoOptions.Seccion).Get<AlmacenamientoOptions>() ?? new();

        switch (opciones.Proveedor)
        {
            case ProveedoresAlmacenamiento.Local:
                var ruta = Path.IsPathRooted(opciones.Local.Ruta)
                    ? opciones.Local.Ruta
                    : Path.Combine(environment.ContentRootPath, opciones.Local.Ruta);
                services.AddSingleton<IAlmacenamientoArchivos>(new AlmacenamientoLocal(ruta));
                break;

            default:
                throw new InvalidOperationException(
                    $"El proveedor de almacenamiento '{opciones.Proveedor}' no está implementado. " +
                    $"Valores admitidos en \"{AlmacenamientoOptions.Seccion}:Proveedor\": {ProveedoresAlmacenamiento.Local}.");
        }

        return services;
    }
}
