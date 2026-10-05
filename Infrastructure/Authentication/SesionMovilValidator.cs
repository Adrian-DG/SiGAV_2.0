using System.Globalization;
using Application.Contracts.Authentication;
using Infrastructure.Persistance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Authentication;

/// <summary>
/// El token móvil dura horas: si front desk desautoriza o desactiva al agente mientras tiene una
/// sesión abierta, sus peticiones deben dejar de funcionar de inmediato, no al vencer el token.
/// En cada petición con sesión móvil se comprueba que el agente siga activo y autorizado (una
/// consulta por clave primaria); si no, la petición recibe 401 y la app cierra la sesión.
/// </summary>
internal static class SesionMovilValidator
{
    public const string MotivoItem = "sigav.motivo401";
    public const string MensajeRevocado = "Su acceso a la aplicación fue revocado. Comuníquese con front desk.";

    public static async Task ValidarAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal?.FindFirst(SesionClaims.TipoSesion)?.Value != TiposSesion.Movil) return;

        var vigente = int.TryParse(principal.FindFirst(SesionClaims.Subject)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var agenteId)
            && await context.HttpContext.RequestServices.GetRequiredService<SiGAVContext>().Agentes
                .AsNoTracking()
                .AnyAsync(a => a.Id == agenteId && a.IsActive && a.Autorizado, context.HttpContext.RequestAborted);

        if (!vigente)
        {
            context.HttpContext.Items[MotivoItem] = MensajeRevocado;
            context.Fail(MensajeRevocado);
        }
    }
}
