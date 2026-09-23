using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Presentation.Middleware;

/// <summary>
/// Traduce las excepciones de aplicación/dominio a respuestas HTTP con el formato { message, errors }.
/// </summary>
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, errors) = exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest, ex.Errors),
            DomainException => (StatusCodes.Status400BadRequest, null),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, null),
            ForbiddenException => (StatusCodes.Status403Forbidden, null),
            NotFoundException => (StatusCodes.Status404NotFound, null),
            ConflictException => (StatusCodes.Status409Conflict, null),
            _ => (StatusCodes.Status500InternalServerError, (IDictionary<string, string[]>?)null)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            // Errores no controlados: se registran y no se expone el detalle al cliente
            logger.LogError(exception, "Error no controlado procesando {Path}", context.Request.Path);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            message = status == StatusCodes.Status500InternalServerError
                ? "Ocurrió un error inesperado al procesar la solicitud."
                : exception.Message,
            errors
        }, cancellationToken);

        return true;
    }
}
