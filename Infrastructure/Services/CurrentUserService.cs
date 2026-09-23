using System.Security.Claims;
using Application.Contracts;
using Application.Contracts.Authentication;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public string? TipoSesion => IsAuthenticated ? User!.FindFirst(SesionClaims.TipoSesion)?.Value : null;

    public int? UserId => TipoSesion == TiposSesion.Web ? GetInt(SesionClaims.Subject) : null;

    public int? AgenteId => TipoSesion == TiposSesion.Movil ? GetInt(SesionClaims.Subject) : null;

    public int? UnidadId => TipoSesion == TiposSesion.Movil ? GetInt(SesionClaims.UnidadId) : null;

    public string? Ficha => TipoSesion == TiposSesion.Movil ? User!.FindFirst(SesionClaims.Ficha)?.Value : null;

    public string? Nombre => IsAuthenticated ? User!.FindFirst(SesionClaims.NombreCompleto)?.Value : null;

    public IReadOnlyCollection<string> Permisos => IsAuthenticated
        ? User!.FindAll(SesionClaims.Permiso).Select(c => c.Value).ToArray()
        : [];

    private int? GetInt(string claim)
        => int.TryParse(User?.FindFirst(claim)?.Value, out var value) ? value : null;
}
