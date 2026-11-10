using Application.Contracts.Authentication;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Seeding.Identity;

/// <summary>
/// Crea como roles de Identity los permisos de <see cref="Permisos.Todos"/> que falten y mantiene
/// su descripción al día. A diferencia de los catálogos, corre en cada arranque aunque ya haya
/// permisos: así uno nuevo en el código queda disponible sin tocar la base.
/// </summary>
internal sealed class PermisosSeeder(SiGAVContext context, RoleManager<AppPermission> roleManager) : ISeeder
{
    public SeedCategoria Categoria => SeedCategoria.Identidad;
    public int Orden => 90;

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var existentes = await context.Roles.ToDictionaryAsync(r => r.Name!, cancellationToken);
        var cambios = 0;

        foreach (var permiso in Permisos.Todos)
        {
            if (existentes.TryGetValue(permiso.Nombre, out var rol))
            {
                if (rol.Description == permiso.Descripcion) continue;
                rol.Description = permiso.Descripcion;
                Verificar(await roleManager.UpdateAsync(rol), permiso.Nombre);
            }
            else
            {
                var nuevo = new AppPermission { Name = permiso.Nombre, Description = permiso.Descripcion };
                Verificar(await roleManager.CreateAsync(nuevo), permiso.Nombre);
                await OtorgarAUsuariosDeAsync(permiso.AlCrearOtorgarA, nuevo, existentes, cancellationToken);
            }

            cambios++;
        }

        return cambios;
    }

    /// <summary>Un permiso separado de otro: quienes tenían el original lo reciben (solo al crearlo).</summary>
    private async Task OtorgarAUsuariosDeAsync(
        string? origen, AppPermission nuevo, Dictionary<string, AppPermission> existentes, CancellationToken cancellationToken)
    {
        if (origen is null || !existentes.TryGetValue(origen, out var rolOrigen)) return;

        var usuarios = await context.UserRoles
            .Where(ur => ur.RoleId == rolOrigen.Id)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);
        if (usuarios.Count == 0) return;

        context.UserRoles.AddRange(usuarios.Select(u => new IdentityUserRole<int> { UserId = u, RoleId = nuevo.Id }));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void Verificar(IdentityResult resultado, string permiso)
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo registrar el permiso {permiso}: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }
}
