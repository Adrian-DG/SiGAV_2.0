using Application.Contracts.Authentication;
using Domain.Enums;
using Infrastructure.Identity;
using Infrastructure.Persistance.Seeding.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistance.Seeding.Identity;

/// <summary>
/// Primer usuario de la web, para poder entrar en una base nueva. Solo se crea si no existe
/// ningún usuario y hay contraseña configurada (Seed:Admin:Password), con todos los permisos.
/// Si en una base existente nadie tiene <see cref="Permisos.UsuariosGestionar"/> (nadie podría
/// asignar permisos desde el front desk), el usuario Seed:Admin:UserName los recibe todos.
/// </summary>
internal sealed class AdministradorSeeder(
    SiGAVContext context,
    UserManager<AppUser> userManager,
    IOptions<SeedingOptions> options,
    ILogger<AdministradorSeeder> logger) : ISeeder
{
    private const string RangoAdministrador = "ASIMILADO";
    // El primero del catálogo (PersonalData.Departamentos), para no depender de un nombre fijo
    private static readonly string DepartamentoAdministrador = PersonalData.Departamentos[0];

    public SeedCategoria Categoria => SeedCategoria.Identidad;
    public int Orden => 100;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var admin = options.Value.Admin;

        if (await context.Users.AnyAsync(cancellationToken))
        {
            if ((await userManager.GetUsersInRoleAsync(Permisos.UsuariosGestionar)).Count > 0) return 0;

            var existente = await userManager.FindByNameAsync(admin.UserName);
            if (existente is null) return 0;

            logger.LogWarning("Ningún usuario puede gestionar permisos; se asignan todos a {Usuario}.", admin.UserName);
            return await AsignarTodosLosPermisosAsync(existente);
        }

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

        return 1 + await AsignarTodosLosPermisosAsync(usuario);
    }

    /// <summary>Los permisos los crea antes <see cref="PermisosSeeder"/> (Orden menor).</summary>
    private async Task<int> AsignarTodosLosPermisosAsync(AppUser usuario)
    {
        var actuales = await userManager.GetRolesAsync(usuario);
        var faltantes = Permisos.Todos.Select(p => p.Nombre).Except(actuales).ToList();
        if (faltantes.Count == 0) return 0;

        var resultado = await userManager.AddToRolesAsync(usuario, faltantes);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                "No se pudieron asignar los permisos al administrador: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));

        return faltantes.Count;
    }
}
