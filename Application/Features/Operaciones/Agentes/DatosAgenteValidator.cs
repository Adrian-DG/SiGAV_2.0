using Application.Contracts.Operaciones;
using Domain.Abstraction;
using Domain.Entities.Operaciones;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Operaciones.Agentes;

public interface IDatosAgente
{
    string Identificacion { get; }
    string Nombre { get; }
    string Apellido { get; }
    SexoEnum Sexo { get; }
    InstitucionEnum Institucion { get; }
    int RangoId { get; }
    AreaOperativaEnum AreaOperativa { get; }
    string? Especialidad { get; }
}

/// <summary>Reglas comunes a crear, registrar y editar un agente.</summary>
public abstract class DatosAgenteValidator<T> : AbstractValidator<T> where T : IDatosAgente
{
    protected DatosAgenteValidator(ICatalogoQueries catalogos)
    {
        RuleFor(x => x.Identificacion)
            .NotEmpty().WithMessage("La cédula es requerida.")
            .Must(c => SoloAlfanumericos(c).Length is >= PersonMetadata.IdentificacionMinLength and <= PersonMetadata.IdentificacionMaxLength)
            .WithMessage($"La cédula debe tener entre {PersonMetadata.IdentificacionMinLength} y {PersonMetadata.IdentificacionMaxLength} caracteres (sin guiones).");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(PersonMetadata.NombreMaxLength).WithMessage($"El nombre no puede exceder {PersonMetadata.NombreMaxLength} caracteres.");
        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es requerido.")
            .MaximumLength(PersonMetadata.ApellidoMaxLength).WithMessage($"El apellido no puede exceder {PersonMetadata.ApellidoMaxLength} caracteres.");
        RuleFor(x => x.Sexo).IsInEnum().WithMessage("El sexo no es válido.");
        RuleFor(x => x.Institucion)
            .IsInEnum().WithMessage("La institución no es válida.")
            .NotEqual(InstitucionEnum.NONE).WithMessage("La institución es requerida.");
        RuleFor(x => x.AreaOperativa).IsInEnum().WithMessage("El área operativa no es válida.");
        RuleFor(x => x.RangoId)
            .GreaterThan(0).WithMessage("El rango es requerido.")
            .MustAsync(catalogos.ExisteRangoAsync).WithMessage("El rango especificado no existe.");
        RuleFor(x => x.Especialidad)
            .MaximumLength(Agente.EspecialidadMaxLength).WithMessage($"La especialidad no puede exceder {Agente.EspecialidadMaxLength} caracteres.");
    }

    private static string SoloAlfanumericos(string? valor) => new((valor ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
}
