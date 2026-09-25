using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistance.Seeding.Identity;

/// <summary>
/// Primer usuario de la web, para poder entrar en una base nueva. Solo se crea si no existe
/// ningún usuario y hay contraseña configurada (Seed:Admin:Password).
/// </summary>
internal sealed class AdministradorSeeder(
    SiGAVContext context,
    UserManager<AppUser> userManager,
    IOptions<SeedingOptions> options,
    ILogger<AdministradorSeeder> logger) : ISeeder
{
    private const string RangoAdministrador = "ASIMILADO";
    private const string DepartamentoAdministrador = "Tecnología";

    public SeedCategoria Categoria => SeedCategoria.Identidad;
    public int Orden => 100;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken)) return 0;

        var admin = options.Value.Admin;
        if (string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning("No hay usuarios y Seed:Admin:Password no está configurada; no se crea el administrador.");
            return 0;
        }

        var rangoId = await context.Rangos.Where(r => r.Nombre == RangoAdministrador).Select(r => r.Id).SingleAsync(cancellationToken);
        var departamentoId = await context.Departamentos.Where(d => d.Nombre == DepartamentoAdministrador).Select(d => d.Id).SingleAsync(cancellationToken);

        var usuario = new AppUser
        {
            UserName = admin.UserName,
            Identificacion = admin.Identificacion,
            Nombre = admin.Nombre,
            Apellido = admin.Apellido,
            Institucion = InstitucionEnum.MOPC,
            RangoId = rangoId,
            DepartamentoId = departamentoId
        };

        var resultado = await userManager.CreateAsync(usuario, admin.Password);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                "No se pudo crear el administrador: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));

        return 1;
    }
}
