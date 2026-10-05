namespace Application.Contracts.Authentication;

/// <summary>
/// Permisos de los usuarios web (front desk). Cada uno es un rol de Identity (AppPermission) y
/// viaja en el JWT web como claim "permission". FrontDesk/src/app/core/permissions/permisos.ts
/// repite estos mismos nombres: al agregar uno aquí, agréguelo también allá.
/// </summary>
public static class Permisos
{
    public const string EventosVer = "eventos.ver";
    public const string AgentesVer = "agentes.ver";
    public const string UnidadesVer = "unidades.ver";
    public const string UsuariosGestionar = "usuarios.gestionar";

    /// <summary>Catálogo completo: se crea como roles al arrancar (PermisosSeeder) y genera una política por permiso.</summary>
    public static readonly IReadOnlyList<PermisoInfo> Todos =
    [
        new(EventosVer, "Eventos", "Consultar asistencias y accidentes reportados por las unidades."),
        new(AgentesVer, "Agentes", "Consultar el personal operativo y su autorización."),
        new(UnidadesVer, "Unidades y denominaciones", "Consultar unidades, denominaciones y tramos."),
        new(UsuariosGestionar, "Usuarios y permisos", "Asignar permisos a los usuarios del front desk."),
    ];

    public static bool Existe(string permiso) => Todos.Any(p => p.Nombre == permiso);
}

public sealed record PermisoInfo(string Nombre, string Modulo, string Descripcion);
