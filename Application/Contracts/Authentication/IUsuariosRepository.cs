using Application.Common.Models;
using Application.Features.Usuarios;

namespace Application.Contracts.Authentication;

/// <summary>Usuarios web (ASP.NET Identity, en Infrastructure) y sus permisos.</summary>
public interface IUsuariosRepository
{
    Task<PagedResult<UsuarioViewModel>> GetUsuariosAsync(string? searchTerm, int page, int size, CancellationToken cancellationToken);

    /// <returns>null si el usuario no existe.</returns>
    Task<IReadOnlyList<string>?> GetPermisosAsync(int usuarioId);

    /// <summary>Deja al usuario exactamente con <paramref name="permisos"/> entre los de <see cref="Permisos.Todos"/>.</summary>
    Task AsignarPermisosAsync(int usuarioId, IReadOnlyCollection<string> permisos);

    Task<bool> ExisteUserNameAsync(string userName);

    Task<bool> ExisteIdentificacionAsync(string identificacion, CancellationToken cancellationToken);

    /// <summary>Crea el usuario con su contraseña y permisos iniciales, todo o nada.</summary>
    /// <returns>Id del usuario creado.</returns>
    Task<int> CrearAsync(NuevoUsuario usuario, string password, IReadOnlyCollection<string> permisos);
}
