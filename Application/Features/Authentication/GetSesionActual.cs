using Application.Contracts;
using Application.Exceptions;
using MediatR;

namespace Application.Features.Authentication;

// Query: identidad de la sesión que hace la petición (útil para que cada cliente se hidrate)
public record GetSesionActualQuery : IRequest<SesionViewModel>;

public record SesionViewModel(
    string TipoSesion,
    string? Nombre,
    int? UserId,
    IReadOnlyCollection<string> Permisos,
    int? AgenteId,
    int? UnidadId,
    string? Ficha);

// Handler
public class GetSesionActualQueryHandler(ICurrentUserService currentUser) : IRequestHandler<GetSesionActualQuery, SesionViewModel>
{
    public Task<SesionViewModel> Handle(GetSesionActualQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.TipoSesion is null)
            throw new UnauthorizedException("No hay una sesión activa.");

        return Task.FromResult(new SesionViewModel(
            currentUser.TipoSesion,
            currentUser.Nombre,
            currentUser.UserId,
            currentUser.Permisos,
            currentUser.AgenteId,
            currentUser.UnidadId,
            currentUser.Ficha));
    }
}
