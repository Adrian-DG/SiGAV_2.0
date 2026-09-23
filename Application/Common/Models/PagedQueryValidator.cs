using FluentValidation;

namespace Application.Common.Models;

public interface IPagedQuery
{
    int Page { get; }
    int Size { get; }
}

/// <summary>
/// Reglas de paginación compartidas por todas las consultas paginadas.
/// </summary>
public class PagedQueryValidator<T> : AbstractValidator<T> where T : IPagedQuery
{
    public const int MaxSize = 100;

    public PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");
        RuleFor(x => x.Size).InclusiveBetween(1, MaxSize).WithMessage($"El tamaño de página debe estar entre 1 y {MaxSize}.");
    }
}
