using Application.Contracts.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

public static class JwtAuthenticationExtensions
{
    /// <summary>Prefijo de los hubs de SignalR (ver Presentation/Hubs).</summary>
    public const string RutaHubs = "/hubs";

    /// <summary>
    /// Un único esquema JwtBearer valida los dos tipos de token (misma llave y emisor,
    /// audiencias distintas). Las políticas exigen el tipo de sesión y la audiencia que
    /// le corresponde, así que un token móvil no sirve en endpoints web ni viceversa.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Conserva los nombres de claim del token ("sub", "permission"...) sin traducirlos a ClaimTypes
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = [jwt.Web.Audience, jwt.Movil.Audience],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                    NameClaimType = SesionClaims.Name,
                    RoleClaimType = SesionClaims.Permiso
                };

                bearer.Events = new JwtBearerEvents
                {
                    // SignalR (WebSockets/SSE) no puede enviar el header Authorization: el token llega
                    // en access_token, y solo se acepta así en las rutas de los hubs
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(RutaHubs))
                            context.Token = token;
                        return Task.CompletedTask;
                    },

                    // Sesión móvil de un agente desautorizado o desactivado: deja de valer al momento
                    OnTokenValidated = SesionMovilValidator.ValidarAsync,

                    // Mismo formato { message } que el resto de errores de la API
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = context.HttpContext.Items[SesionMovilValidator.MotivoItem] as string
                                ?? (context.AuthenticateFailure is SecurityTokenExpiredException
                                    ? "La sesión expiró, inicie sesión nuevamente."
                                    : "Se requiere una sesión válida.")
                        });
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = "La sesión actual no tiene permiso para esta operación."
                        });
                    }
                };
            });

        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>()
            .Configure<IOptions<JwtOptions>>((authorization, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                authorization.AddPolicy(SesionPolicies.Web, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(SesionClaims.TipoSesion, TiposSesion.Web)
                    .RequireClaim(SesionClaims.Audience, jwt.Web.Audience));

                authorization.AddPolicy(SesionPolicies.Movil, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(SesionClaims.TipoSesion, TiposSesion.Movil)
                    .RequireClaim(SesionClaims.Audience, jwt.Movil.Audience));

                authorization.AddPolicy(SesionPolicies.Operativa, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        (context.User.HasClaim(SesionClaims.TipoSesion, TiposSesion.Web)
                            && context.User.HasClaim(SesionClaims.Audience, jwt.Web.Audience))
                        || (context.User.HasClaim(SesionClaims.TipoSesion, TiposSesion.Movil)
                            && context.User.HasClaim(SesionClaims.Audience, jwt.Movil.Audience))));

                // Una política por permiso (SesionPolicies.ConPermiso): sesión web con ese claim "permission"
                foreach (var permiso in Permisos.Todos.Select(p => p.Nombre))
                    authorization.AddPolicy(SesionPolicies.ConPermiso(permiso), policy => policy
                        .RequireAuthenticatedUser()
                        .RequireClaim(SesionClaims.TipoSesion, TiposSesion.Web)
                        .RequireClaim(SesionClaims.Audience, jwt.Web.Audience)
                        .RequireClaim(SesionClaims.Permiso, permiso));
            });

        return services;
    }
}
