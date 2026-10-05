using Application.Common;
using Application.Contracts;
using Application.Contracts.Authentication;
using Application.Exceptions;
using Domain.Abstraction;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Usuarios;

/// <summary>Datos de un usuario web nuevo, ya validados (la cédula sin guiones).</summary>
public record NuevoUsuario(
    string UserName,
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    int RangoId,
    int DepartamentoId);

// Command: alta de un usuario del front desk, con sus permisos iniciales
public record CrearUsuarioCommand(
    string UserName,
    string Password,
    string Identificacion,
    string Nombre,
    string Apellido,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    int RangoId,
    int DepartamentoId,
    IReadOnlyList<string>? Permisos) : IRequest<int>
{
    public string IdentificacionNormalizada => new((Identificacion ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
}

public class CrearUsuarioCommandValidator : AbstractValidator<CrearUsuarioCommand>
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 50;
    public const int PasswordMinLength = 8;

    public CrearUsuarioCommandValidator(IReadDbContext db)
    {
        // Cascade(Stop): un mensaje por campo (el primero que falle), que es lo que muestra el formulario
        // Subconjunto de los caracteres que Identity admite por defecto en un UserName
        RuleFor(x => x.UserName).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El usuario es requerido.")
            .Length(UserNameMinLength, UserNameMaxLength).WithMessage($"El usuario debe tener entre {UserNameMinLength} y {UserNameMaxLength} caracteres.")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("El usuario solo puede tener letras sin acentos, números, punto, guion y guion bajo.");

        // Igual o más estricta que la política por defecto de Identity, para no recibir sus errores (en inglés)
        RuleFor(x => x.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(PasswordMinLength).WithMessage($"La contraseña debe tener al menos {PasswordMinLength} caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe tener al menos un símbolo (por ejemplo: . - _ @ #).");

        RuleFor(x => x.IdentificacionNormalizada)
            .Must(c => c.Length is >= PersonMetadata.IdentificacionMinLength and <= PersonMetadata.IdentificacionMaxLength)
            .WithMessage($"La cédula debe tener entre {PersonMetadata.IdentificacionMinLength} y {PersonMetadata.IdentificacionMaxLength} caracteres (sin guiones).")
            .OverridePropertyName(nameof(CrearUsuarioCommand.Identificacion));
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(PersonMetadata.NombreMaxLength).WithMessage($"El nombre no puede exceder {PersonMetadata.NombreMaxLength} caracteres.");
        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(PersonMetadata.ApellidoMaxLength).WithMessage($"El apellido no puede exceder {PersonMetadata.ApellidoMaxLength} caracteres.");
        RuleFor(x => x.Sexo).Cascade(CascadeMode.Stop)
            .IsInEnum().WithMessage("El sexo no es válido.")
            .NotEqual(SexoEnum.NONE).WithMessage("El sexo es requerido.");
        RuleFor(x => x.Institucion).Cascade(CascadeMode.Stop)
            .IsInEnum().WithMessage("La institución no es válida.")
            .NotEqual(InstitucionEnum.NONE).WithMessage("La institución es requerida.");
        RuleFor(x => x.RangoId).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("El rango es requerido.")
            .MustAsync((id, ct) => db.Rangos.ExisteActivoAsync(id, ct)).WithMessage("El rango especificado no existe.");
        RuleFor(x => x.DepartamentoId).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("El departamento es requerido.")
            .MustAsync((id, ct) => db.Departamentos.ExisteActivoAsync(id, ct)).WithMessage("El departamento especificado no existe.");

        RuleForEach(x => x.Permisos)
            .Must(Contracts.Authentication.Permisos.Existe)
            .WithMessage("El permiso '{PropertyValue}' no existe.");
        RuleFor(x => x.Permisos)
            .Must(p => p!.Distinct().Count() == p!.Count)
            .When(x => x.Permisos is not null)
            .WithMessage("Hay permisos repetidos.");
    }
}

public class CrearUsuarioCommandHandler(IUsuariosRepository repository) : IRequestHandler<CrearUsuarioCommand, int>
{
    public async Task<int> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var identificacion = request.IdentificacionNormalizada;

        if (await repository.ExisteUserNameAsync(userName))
            throw new ConflictException($"Ya existe un usuario '{userName}'.");
        if (await repository.ExisteIdentificacionAsync(identificacion, cancellationToken))
            throw new ConflictException("Ya existe un usuario con esa cédula.");

        return await repository.CrearAsync(
            new NuevoUsuario(
                userName,
                identificacion,
                request.Nombre.Trim(),
                request.Apellido.Trim(),
                request.Sexo,
                request.Institucion,
                request.RangoId,
                request.DepartamentoId),
            request.Password,
            request.Permisos ?? []);
    }
}

public record RangoItemViewModel(int Id, string Nombre, string NombreArmada);
public record DepartamentoItemViewModel(int Id, string Nombre);
public record CatalogosUsuarioViewModel(IReadOnlyList<RangoItemViewModel> Rangos, IReadOnlyList<DepartamentoItemViewModel> Departamentos);

// Query: listas del formulario de alta (rangos activos y departamentos activos)
public record GetCatalogosUsuarioQuery : IRequest<CatalogosUsuarioViewModel>;

public class GetCatalogosUsuarioQueryHandler(IReadDbContext db) : IRequestHandler<GetCatalogosUsuarioQuery, CatalogosUsuarioViewModel>
{
    public async Task<CatalogosUsuarioViewModel> Handle(GetCatalogosUsuarioQuery request, CancellationToken cancellationToken)
    {
        // Rangos en el orden del catálogo (jerárquico), no alfabético
        var rangos = await db.Rangos
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .Select(r => new RangoItemViewModel(r.Id, r.Nombre, r.NombreArmada))
            .ToListAsync(cancellationToken);
        var departamentos = await db.Departamentos
            .Where(d => d.IsActive)
            .OrderBy(d => d.Nombre)
            .Select(d => new DepartamentoItemViewModel(d.Id, d.Nombre))
            .ToListAsync(cancellationToken);

        return new CatalogosUsuarioViewModel(rangos, departamentos);
    }
}
