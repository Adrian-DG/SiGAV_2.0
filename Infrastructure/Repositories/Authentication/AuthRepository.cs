using Application.Contracts.Authentication;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Repositories.Authentication;

public class AuthRepository(UserManager<AppUser> userManager, IJwtBearerHelper jwtBearerHelper) : IAuthRepository
{
    public async Task<string> LoginAsync(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        
        if (user is null) throw new Exception("User not found.");

        if (!await userManager.CheckPasswordAsync(user, password)) throw new Exception("Invalid password.");
        
        var permissions = await userManager.GetRolesAsync(user);
        
        return jwtBearerHelper.GenerateToken(user.Id, user.UsuarioInfo, permissions);
    }
}