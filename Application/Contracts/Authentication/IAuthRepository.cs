namespace Application.Contracts.Authentication;

public interface IAuthRepository
{
    Task<string> LoginAsync(string username, string password);
}