namespace Application.Contracts;

/// <summary>
/// Identidad de quien hace la petición, leída del JWT.
/// </summary>
public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    /// <summary><c>web</c>, <c>movil</c> o null si la petición es anónima.</summary>
    string? TipoSesion { get; }

    /// <summary>Id del usuario web (AppUser), o null si no es una sesión web.</summary>
    int? UserId { get; }

    /// <summary>Id del agente, o null si no es una sesión móvil.</summary>
    int? AgenteId { get; }

    /// <summary>Unidad con la que inició sesión el agente, o null si no es una sesión móvil.</summary>
    int? UnidadId { get; }

    string? Ficha { get; }
    string? Nombre { get; }
    IReadOnlyCollection<string> Permisos { get; }
}
