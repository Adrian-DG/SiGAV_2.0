using Application.Features.Authentication;

namespace Application.Contracts.Authentication;

public interface IAuthRepository
{
    Task<AuthenticatedResponse> LoginAsync(string username, string password);
    Task RegisterAsync(RegisterUserCommand command);
}