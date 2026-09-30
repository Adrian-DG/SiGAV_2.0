using Application.Contracts;
using Application.Contracts.Operaciones;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Operaciones.Agente;

public record CreateAgenteCommand(
	string identificacion,
	string nombre,
	string apellido,
	SexoEnum Sexo,
	InstitucionEnum Institucion,
	int RangoId
) : IRequest;


public class CreateAgenteCommandValidator : AbstractValidator<CreateAgenteCommand>
{
	public CreateAgenteCommandValidator()
	{
		RuleFor(x => x.identificacion)
			.NotEmpty()
			.Length(8, 11);

		RuleFor(x => x.nombre)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.apellido)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.Sexo)
			.IsInEnum();

		RuleFor(x => x.Institucion)
			.IsInEnum();

		RuleFor(x => x.RangoId)
			.GreaterThan(0);
	}
}

public class CreateAgenteCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateAgenteCommand>
{
	public async Task<Unit> Handle(CreateAgenteCommand request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}