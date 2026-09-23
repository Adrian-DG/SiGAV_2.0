using Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    // Claim emitido por JwtBearerHelper
    private const string UserIdClaim = "id";

    public int? UserId =>
        int.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(UserIdClaim)?.Value, out var id) ? id : null;
}
