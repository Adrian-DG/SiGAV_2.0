using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Presentation.OpenApi;

/// <summary>
/// Declara en el documento OpenAPI el esquema de seguridad JWT Bearer, para que Scalar
/// permita autenticarse. Solo se agrega si el esquema JwtBearer está registrado en la API.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider) : IOpenApiDocumentTransformer
{
    public const string SchemeName = JwtBearerDefaults.AuthenticationScheme;

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == SchemeName)) return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = """
                Token JWT emitido por la API. Hay dos tipos de sesión:
                - **Front desk (web):** `POST /api/authentication/login` con usuario y contraseña.
                - **App móvil:** `POST /api/authentication/movil/login` con cédula y ficha.

                Copie el valor `token` de la respuesta (sin el prefijo `Bearer`).
                """
        };
    }
}
