using Application.Common.Models;
using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Exceptions;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Usuarios;

public record UsuarioViewModel(
    int Id,
    string UserName,
    string NombreCompleto,
    string Identificacion,
    InstitucionEnum Institucion,
    string Rango,
    string Departamento,
    bool Bloqueado,
    IReadOnlyList<string> Permisos);

// Query: usuarios web paginados, con sus permisos
public record GetUsuariosQuery(int Page = 1, int Size = 20, string? SearchTerm = null)
    : IRequest<PagedResult<UsuarioViewModel>>, IPagedQuery;

public class GetUsuariosQueryValidator : PagedQueryValidator<GetUsuariosQuery>;

public class GetUsuariosQueryHandler(IUsuariosRepository repository)
    : IRequestHandler<GetUsuariosQuery, PagedResult<UsuarioViewModel>>
{
    public Task<PagedResult<UsuarioViewModel>> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
        => repository.GetUsuariosAsync(request.SearchTerm, request.Page, request.Size, cancellationToken);
}

// Query: catálogo de permisos que se pueden asignar
public record GetPermisosQuery : IRequest<IReadOnlyList<PermisoInfo>>;

public class GetPermisosQueryHandler : IRequestHandler<GetPermisosQuery, IReadOnlyList<PermisoInfo>>
{
    public Task<IReadOnlyList<PermisoInfo>> Handle(GetPermisosQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Permisos.Todos);
}

// Command: reemplaza los permisos del usuario. Los cambios llegan a su sesión en el próximo login
// (los permisos viajan en el token).
public record AsignarPermisosUsuarioCommand(int UsuarioId, IReadOnlyList<string> Permisos) : IRequest;

public class AsignarPermisosUsuarioCommandValidator : AbstractValidator<AsignarPermisosUsuarioCommand>
{
    public AsignarPermisosUsuarioCommandValidator()
    {
        RuleFor(x => x.Permisos).NotNull().WithMessage("Indique los permisos del usuario.");
        RuleForEach(x => x.Permisos)
            .Must(Contracts.Authentication.Permisos.Existe)
            .WithMessage("El permiso '{PropertyValue}' no existe.");
        RuleFor(x => x.Permisos)
            .Must(p => p.Distinct().Count() == p.Count)
            .When(x => x.Permisos is not null)
            .WithMessage("Hay permisos repetidos.");
    }
}

public class AsignarPermisosUsuarioCommandHandler(IUsuariosRepository repository, ICurrentUserService currentUser)
    : IRequestHandler<AsignarPermisosUsuarioCommand, Unit>
{
    public async Task<Unit> Handle(AsignarPermisosUsuarioCommand request, CancellationToken cancellationToken)
    {
        if (await repository.GetPermisosAsync(request.UsuarioId) is null)
            throw new NotFoundException("El usuario no existe.");

        // Quien asigna siempre conserva el permiso: así nunca queda el sistema sin nadie que pueda asignarlos
        if (request.UsuarioId == currentUser.UserId && !request.Permisos.Contains(Contracts.Authentication.Permisos.UsuariosGestionar))
            throw new ConflictException("No puede quitarse a sí mismo el permiso de gestionar usuarios y permisos.");

        await repository.AsignarPermisosAsync(request.UsuarioId, request.Permisos);
        return Unit.Value;
    }
}
