using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Application.Contracts.Authentication;
using Application.Features.Authentication;
using Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Repositories.Authentication;

public class JwtBearerHelper(IConfiguration configuration) : IJwtBearerHelper
{
    private const int ExpirationHours = 12;

    public AuthenticatedResponse GenerateToken(int userId, string username, IList<string> permissions)
    {
        string? secretKey = configuration["Jwt:SecretKey"] ?? throw new Exception("Secret key not found in configuration.");
        
        var key = Encoding.ASCII.GetBytes(secretKey!);

        var tokenHanler = new JwtSecurityTokenHandler();
        var expiration = DateTime.UtcNow.AddHours(ExpirationHours);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {   
                new System.Security.Claims.Claim("id", userId.ToString()),
                new System.Security.Claims.Claim("username", username),
                new System.Security.Claims.Claim("permissions", string.Join(",", permissions))
            }),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Expires = expiration,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHanler.CreateToken(tokenDescriptor);
        return new AuthenticatedResponse(tokenHanler.WriteToken(token), expiration);
    }
}