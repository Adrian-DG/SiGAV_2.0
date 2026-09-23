using Application.Features.Authentication;

namespace Application.Contracts.Authentication;

public interface IJwtBearerHelper
{
    AuthenticatedResponse GenerateToken(int userId, string username, IList<string> permissions);
}