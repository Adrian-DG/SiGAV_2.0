using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Exceptions;
using Domain.ValueObjects;

namespace Application.Common;

public static class AutorCambioExtensions
{
    /// <summary>
    /// Autor de un cambio de denominación. Solo un usuario de la aplicación web (front desk)
    /// puede cambiar la denominación de una unidad; cualquier otra sesión recibe 403.
    /// </summary>
    public static AutorCambio RequerirAutorWeb(this ICurrentUserService currentUser, TimeProvider timeProvider, string? observacion = null)
    {
        if (currentUser.TipoSesion != TiposSesion.Web || currentUser.UserId is not { } usuarioId)
            throw new ForbiddenException("Los cambios de denominación solo pueden realizarse desde la aplicación web (front desk).");

        return new AutorCambio(usuarioId, timeProvider.GetUtcNow().UtcDateTime, observacion);
    }
}
