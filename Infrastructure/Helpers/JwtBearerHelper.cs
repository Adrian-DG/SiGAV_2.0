using System.Globalization;
using System.Security.Claims;
using Application.Contracts.Authentication;
using Application.Features.Authentication;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Helpers;

public class JwtBearerHelper(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtBearerHelper
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AuthenticatedResponse GenerateWebToken(WebUserIdentity identity)
    {
        var claims = new List<Claim>
        {
            new(SesionClaims.Subject, identity.UserId.ToString(CultureInfo.InvariantCulture)),
            new(SesionClaims.TipoSesion, TiposSesion.Web),
            new(SesionClaims.Name, identity.UserName),
            new(SesionClaims.NombreCompleto, identity.NombreCompleto),
            new(SesionClaims.DepartamentoId, identity.DepartamentoId.ToString(CultureInfo.InvariantCulture)),
            new(SesionClaims.Institucion, identity.Institucion.ToString()),
        };

        // Un claim por permiso: se serializa como arreglo y habilita [Authorize(Roles = "...")]
        claims.AddRange(identity.Permisos.Distinct().Select(p => new Claim(SesionClaims.Permiso, p)));

        return BuildToken(claims, _options.Web);
    }

    public AuthenticatedResponse GenerateMovilToken(MovilUserIdentity identity)
    {
        var claims = new List<Claim>
        {
            new(SesionClaims.Subject, identity.AgenteId.ToString(CultureInfo.InvariantCulture)),
            new(SesionClaims.TipoSesion, TiposSesion.Movil),
            new(SesionClaims.Name, identity.Identificacion),
            new(SesionClaims.NombreCompleto, identity.NombreCompleto),
            new(SesionClaims.Rango, identity.Rango),
            new(SesionClaims.UnidadId, identity.UnidadId.ToString(CultureInfo.InvariantCulture)),
            new(SesionClaims.Ficha, identity.Ficha),
        };

        return BuildToken(claims, _options.Movil);
    }

    private AuthenticatedResponse BuildToken(List<Claim> claims, JwtAudienceOptions audiencia)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiration = now.AddHours(audiencia.ExpirationHours);

        claims.Add(new Claim(SesionClaims.TokenId, Guid.NewGuid().ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = audiencia.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiration,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
                SecurityAlgorithms.HmacSha256)
        };

        return new AuthenticatedResponse(_handler.CreateToken(descriptor), expiration);
    }
}
