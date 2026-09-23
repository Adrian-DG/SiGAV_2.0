using FluentValidation;
using Application.Contracts;
using Domain.ViewModels;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Misc;

public record GetNamedResourceQuery(string Column, string Table, object? Parameters = null) 
    : IRequest<IEnumerable<NamedViewModel>>;

public class GetNamedResourceQueryValidator : AbstractValidator<GetNamedResourceQuery>
{
    public GetNamedResourceQueryValidator()
    {
        RuleFor(x => x.Column).NotEmpty().WithMessage("Column is required.");
        RuleFor(x => x.Table).NotEmpty().WithMessage("Table is required.");
    }
}

public class GetNamedResourceQueryHandler(IMiscRepository miscRepository) : IRequestHandler<GetNamedResourceQuery, IEnumerable<NamedViewModel>>
{
    public async Task<IEnumerable<NamedViewModel>> Handle(GetNamedResourceQuery request, CancellationToken cancellationToken)
    {
        return await miscRepository.GetNamedResource(request.Column, request.Table, request.Parameters);
    }
}