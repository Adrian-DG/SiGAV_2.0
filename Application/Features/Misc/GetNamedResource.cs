using FluentValidation;
using Infrastructure.Persistance.Misc;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Misc;

public record GetNamedResourceQuery(string Column, string Table, object? Parameters = null) 
    : IRequest<IEnumerable<NamedModel>>;

public class GetNamedResourceQueryValidator : AbstractValidator<GetNamedResourceQuery>
{
    public GetNamedResourceQueryValidator()
    {
        RuleFor(x => x.Column).NotEmpty().WithMessage("Column is required.");
        RuleFor(x => x.Table).NotEmpty().WithMessage("Table is required.");
    }
}

public class GetNamedResourceQueryHandler(IMiscRepository miscRepository) : IRequestHandler<GetNamedResourceQuery, IEnumerable<NamedModel>>
{
    public async Task<IEnumerable<NamedModel>> Handle(GetNamedResourceQuery request, CancellationToken cancellationToken)
    {
        return await miscRepository.GetNamedResource(request.Column, request.Table, request.Parameters);
    }
}