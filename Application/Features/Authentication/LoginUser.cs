using Application.Contracts;
using FluentValidation;
using MediatR;

namespace Application.Features.Authentication;

// Command
public record LoginUserCommand(string username, string password) : IRequest<AuthenticatedResponse>;

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
public class LoginUserCommandHandler(IUnitOfWork uow) : IRequestHandler<LoginUserCommand, AuthenticatedResponse>
{
    public async Task<AuthenticatedResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return await uow.AuthRepository.LoginAsync(request.username, request.password, cancellationToken);
    }
}

// Response
public record AuthenticatedResponse(string token, DateTime expiration);