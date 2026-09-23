using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Infrastructure.Authentication;

/// <summary>
/// Sección "Jwt" de appsettings. La llave nunca se versiona para producción:
/// se inyecta con la variable de entorno <c>Jwt__SecretKey</c>.
/// </summary>
public class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 exige una llave de al menos 256 bits.</summary>
    public const int MinSecretKeyBytes = 32;

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string SecretKey { get; set; } = string.Empty;

    /// <summary>Tolerancia de reloj entre servidor y clientes al validar exp/nbf.</summary>
    [Range(0, 300)] public int ClockSkewSeconds { get; set; } = 60;

    [Required] public JwtAudienceOptions Web { get; set; } = new() { Audience = "sigav-web", ExpirationHours = 8 };
    [Required] public JwtAudienceOptions Movil { get; set; } = new() { Audience = "sigav-movil", ExpirationHours = 12 };

    public byte[] GetSigningKeyBytes() => Encoding.UTF8.GetBytes(SecretKey);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Encoding.UTF8.GetByteCount(SecretKey) < MinSecretKeyBytes)
            yield return new ValidationResult($"Jwt:SecretKey debe tener al menos {MinSecretKeyBytes} bytes.", [nameof(SecretKey)]);

        foreach (var (nombre, audiencia) in new[] { (nameof(Web), Web), (nameof(Movil), Movil) })
        {
            if (string.IsNullOrWhiteSpace(audiencia.Audience))
                yield return new ValidationResult($"Jwt:{nombre}:Audience es requerido.", [nombre]);
            if (audiencia.ExpirationHours is < 1 or > 24)
                yield return new ValidationResult($"Jwt:{nombre}:ExpirationHours debe estar entre 1 y 24.", [nombre]);
        }

        if (string.Equals(Web.Audience, Movil.Audience, StringComparison.Ordinal))
            yield return new ValidationResult("Las audiencias web y móvil deben ser distintas.", [nameof(Web), nameof(Movil)]);
    }
}

public class JwtAudienceOptions
{
    public string Audience { get; set; } = string.Empty;
    public int ExpirationHours { get; set; }
}
