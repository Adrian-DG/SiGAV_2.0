namespace Application.Contracts.Authentication;

/// <summary>
/// Nombres de los claims que viajan en el JWT. Se usan tal cual (sin el mapeo a
/// ClaimTypes de .NET) tanto al emitir como al validar el token.
/// </summary>
public static class SesionClaims
{
    // Estándar (RFC 7519)
    public const string Subject = "sub";
    public const string TokenId = "jti";
    public const string Name = "name";
    public const string Audience = "aud";

    /// <summary>Tipo de sesión: <see cref="TiposSesion.Web"/> o <see cref="TiposSesion.Movil"/>.</summary>
    public const string TipoSesion = "sesion";
    public const string NombreCompleto = "nombre";

    // Sesión web (front desk)
    public const string Permiso = "permission";
    public const string DepartamentoId = "departamentoId";
    public const string Institucion = "institucion";

    // Sesión móvil (unidad en campo)
    public const string UnidadId = "unidadId";
    public const string Ficha = "ficha";
}

public static class TiposSesion
{
    public const string Web = "web";
    public const string Movil = "movil";
}

/// <summary>
/// Políticas de autorización disponibles para los controladores.
/// </summary>
public static class SesionPolicies
{
    /// <summary>Solo usuarios de la aplicación web (front desk).</summary>
    public const string Web = "SesionWeb";

    /// <summary>Solo unidades de la app móvil.</summary>
    public const string Movil = "SesionMovil";

    /// <summary>Cualquier sesión válida, web o móvil.</summary>
    public const string Operativa = "SesionOperativa";
}
