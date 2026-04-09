using Application.Contracts.Authentication;
using Application.Features.Authentication;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Repositories.Authentication;

public class AuthRepository(UserManager<AppUser> userManager, IJwtBearerHelper jwtBearerHelper) : IAuthRepository
{
    public async Task<AuthenticatedResponse> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(username);
        
        if (user is null) throw new Exception("User not found.");

        if (!await userManager.CheckPasswordAsync(user, password)) throw new Exception("Invalid password.");
        
        var permissions = await userManager.GetRolesAsync(user);
        
        return jwtBearerHelper.GenerateToken(user.Id, user.UsuarioInfo, permissions);
    }
}