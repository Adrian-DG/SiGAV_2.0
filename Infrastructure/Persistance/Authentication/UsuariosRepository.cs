using Application.Common;
using Application.Common.Models;
using Application.Contracts.Authentication;
using Application.Features.Usuarios;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Authentication;

public class UsuariosRepository(SiGAVContext context, UserManager<AppUser> userManager, TimeProvider timeProvider) : IUsuariosRepository
{
    public async Task<PagedResult<UsuarioViewModel>> GetUsuariosAsync(string? searchTerm, int page, int size, CancellationToken cancellationToken)
    {
        var query = context.Users.AsNoTracking();

        if (QueryableExtensions.PatronBusqueda(searchTerm) is { } patron)
            query = query.Where(u => EF.Functions.Like(u.UserName!, patron)
                || EF.Functions.Like(u.Identificacion, patron)
                || (u.Nombre != null && EF.Functions.Like(u.Nombre, patron))
                || (u.Apellido != null && EF.Functions.Like(u.Apellido, patron)));

        var pagina = await query
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .ThenBy(u => u.UserName)
            .Select(u => new FilaUsuario(
                u.Id,
                u.UserName!,
                u.Nombre,
                u.Apellido,
                u.Identificacion,
                u.Institucion,
                // La Armada usa sus propios nombres de rango (AppUser.UsuarioInfo)
                u.Institucion == InstitucionEnum.ARD ? u.Rango!.NombreArmada : u.Rango!.Nombre,
                u.Departamento!.Nombre,
                u.LockoutEnd,
                context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
                    .ToList()))
            .ToPagedResultAsync(page, size, cancellationToken);

        var ahora = timeProvider.GetUtcNow();
        var items = pagina.Items.Select(f => new UsuarioViewModel(
                f.Id,
                f.UserName,
                $"{f.Nombre} {f.Apellido}".Trim(),
                f.Identificacion,
                f.Institucion,
                f.Rango,
                f.Departamento,
                f.LockoutEnd > ahora,
                f.Permisos.Order().ToList()))
            .ToList();

        return new PagedResult<UsuarioViewModel>(items, pagina.Page, pagina.Size, pagina.TotalCount);
    }

    public async Task<IReadOnlyList<string>?> GetPermisosAsync(int usuarioId)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        return usuario is null ? null : (await userManager.GetRolesAsync(usuario)).ToList();
    }

    public async Task AsignarPermisosAsync(int usuarioId, IReadOnlyCollection<string> permisos)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"No existe el usuario {usuarioId}.");

        // Solo se tocan los permisos del catálogo; otros roles que tenga el usuario se conservan
        var catalogo = Permisos.Todos.Select(p => p.Nombre).ToHashSet();
        var actuales = (await userManager.GetRolesAsync(usuario)).Where(catalogo.Contains).ToList();

        Verificar(await userManager.RemoveFromRolesAsync(usuario, actuales.Except(permisos)));
        Verificar(await userManager.AddToRolesAsync(usuario, permisos.Except(actuales)));
    }

    public async Task<bool> ExisteUserNameAsync(string userName)
        => await userManager.FindByNameAsync(userName) is not null;

    public Task<bool> ExisteIdentificacionAsync(string identificacion, CancellationToken cancellationToken)
        => context.Users.AnyAsync(u => u.Identificacion == identificacion, cancellationToken);

    public async Task<int> CrearAsync(NuevoUsuario datos, string password, IReadOnlyCollection<string> permisos)
    {
        var usuario = new AppUser
        {
            UserName = datos.UserName,
            Identificacion = datos.Identificacion,
            Nombre = datos.Nombre,
            Apellido = datos.Apellido,
            Sexo = datos.Sexo,
            Institucion = datos.Institucion,
            RangoId = datos.RangoId,
            DepartamentoId = datos.DepartamentoId
        };

        // Si fallan los permisos, el usuario no debe quedar creado a medias
        await using var transaccion = await context.Database.BeginTransactionAsync();
        Verificar(await userManager.CreateAsync(usuario, password), "No se pudo crear el usuario");
        if (permisos.Count > 0)
            Verificar(await userManager.AddToRolesAsync(usuario, permisos));
        await transaccion.CommitAsync();

        return usuario.Id;
    }

    private static void Verificar(IdentityResult resultado, string contexto = "No se pudieron actualizar los permisos")
    {
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                $"{contexto}: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }

    private sealed record FilaUsuario(
        int Id,
        string UserName,
        string? Nombre,
        string? Apellido,
        string Identificacion,
        InstitucionEnum Institucion,
        string Rango,
        string Departamento,
        DateTimeOffset? LockoutEnd,
        List<string> Permisos);
}
