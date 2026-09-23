using Application.Contracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Presentation.OpenApi;

/// <summary>
/// Marca como protegidos solo los endpoints que exigen autorización ([Authorize] sin
/// [AllowAnonymous]): Scalar muestra el candado y envía el token únicamente en esos. También
/// documenta las respuestas 401/403 y qué tipo de sesión exige cada operación.
/// </summary>
internal sealed class AuthorizeOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly Dictionary<string, string> DescripcionPoliticas = new()
    {
        [SesionPolicies.Web] = "solo usuarios de la aplicación web (front desk)",
        [SesionPolicies.Movil] = "solo unidades de la app móvil",
        [SesionPolicies.Operativa] = "cualquier sesión válida (web o móvil)"
    };

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any()) return Task.CompletedTask;

        var authorize = metadata.OfType<IAuthorizeData>().ToList();
        if (authorize.Count == 0) return Task.CompletedTask;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = new List<string>()
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Falta el token, es inválido o expiró." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "La sesión no tiene permiso para esta operación." });

        // Las políticas del controlador y de la acción aplican a la vez. Si hay una específica
        // (Web o Movil), Operativa ya queda implícita y se omite para no confundir.
        var nombres = authorize.Select(a => a.Policy).OfType<string>().Distinct().ToList();
        if (nombres.Contains(SesionPolicies.Web) || nombres.Contains(SesionPolicies.Movil))
            nombres.Remove(SesionPolicies.Operativa);

        var politicas = nombres
            .Select(p => DescripcionPoliticas.TryGetValue(p, out var descripcion) ? $"`{p}`: {descripcion}" : $"`{p}`")
            .ToList();

        if (politicas.Count > 0)
        {
            var requisito = $"**Requiere sesión** — {string.Join("; ", politicas)}.";
            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? requisito
                : $"{operation.Description}\n\n{requisito}";
        }

        return Task.CompletedTask;
    }
}
