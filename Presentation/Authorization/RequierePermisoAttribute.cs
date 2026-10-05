using Application.Contracts.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization;

/// <summary>
/// Exige a la sesión web un permiso de <see cref="Permisos"/> (claim "permission" del JWT).
/// Se suma a la política del controlador: ambas deben cumplirse.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequierePermisoAttribute(string permiso) : AuthorizeAttribute(SesionPolicies.ConPermiso(permiso))
{
    public string Permiso { get; } = permiso;
}
