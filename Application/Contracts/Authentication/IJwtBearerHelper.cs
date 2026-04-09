namespace Application.Contracts.Authentication;

public interface IJwtBearerHelper
{
    string GenerateToken(int userId, string username, IList<string> permissions);
}