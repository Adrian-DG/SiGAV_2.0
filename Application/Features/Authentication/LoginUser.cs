using Application.Contracts;
using FluentValidation;
using MediatR;

namespace Application.Features.Authentication;

// Command
public record LoginUserCommand(string username, string password) : IRequest<string>;

// Validator
public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.password).NotEmpty().WithMessage("Password is required.");
    }
}

// Handler
public class LoginUserCommandHandler(IUnitOfWork uow) : IRequestHandler<LoginUserCommand, string>
{
    public async Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return await uow.AuthRepository.LoginAsync(request.username, request.password);
    }
}