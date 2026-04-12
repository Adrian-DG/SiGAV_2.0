using Application.Contracts;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Authentication;

// Command
public record RegisterUserCommand(
    string identificacion, 
    string nombre, 
    string apellido, 
    SexoEnum sexo,
    int RangoId,
    int DepartamentoId, 
    string username, 
    string password) : IRequest;

// Validator
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.identificacion)
            .NotEmpty().WithMessage("Identificacion is required.")
            .NotNull().WithMessage("Identificacion is required.")
            .Length(11).WithMessage("Identificacion must be exactly 11 characters.");
        RuleFor(x => x.nombre).NotEmpty().WithMessage("Nombre is required.");
        RuleFor(x => x.apellido).NotEmpty().WithMessage("Apellido is required.");
        RuleFor(x => x.sexo).IsInEnum().WithMessage("Sexo must be a valid enum value.");
        RuleFor(x => x.RangoId).GreaterThan(0).WithMessage("RangoId must be greater than 0.");
        RuleFor(x => x.DepartamentoId).GreaterThan(0).WithMessage("DepartamentoId must be greater than 0.");
    }
}

// Handler
public class RegisterCommandHandler(IUnitOfWork uow):  IRequestHandler<RegisterUserCommand>
{
    public async Task Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        await uow.AuthRepository.RegisterAsync(request);
    }
}
    