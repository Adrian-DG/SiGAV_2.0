using Application.Features.Authentication;
using Domain.Enums;

namespace Application.Contracts.Authentication;

public interface IJwtBearerHelper
{
    AuthenticatedResponse GenerateWebToken(WebUserIdentity identity);
    AuthenticatedResponse GenerateMovilToken(MovilUserIdentity identity);
}

/// <summary>
/// Usuario de la aplicación web (front desk), autenticado con ASP.NET Identity.
/// </summary>
public record WebUserIdentity(
    int UserId,
    string UserName,
    string NombreCompleto,
    int DepartamentoId,
    InstitucionEnum Institucion,
    IEnumerable<string> Permisos);

/// <summary>
/// Agente que opera una unidad desde la app móvil. Solo lleva identificadores estables:
/// denominación y tramo cambian con las reasignaciones y se consultan en el servidor.
/// </summary>
public record MovilUserIdentity(
    int AgenteId,
    string Identificacion,
    string Institucion,
    string Rango,
    string NombreCompleto,
    int UnidadId,
    string Ficha,
    ComponenteIdentity? Componente = null);

/// <summary>
/// Segundo agente que opera la unidad junto al que inició sesión. Permite que uno se quede en un
/// evento mientras el otro va a otro: cada evento indica cuál de los dos lo atiende.
/// </summary>
public record ComponenteIdentity(
    int AgenteId,
    string Identificacion,
    string Institucion,
    string Rango,
    string NombreCompleto);
