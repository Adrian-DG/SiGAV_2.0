using Application.Contracts.Authentication;
using Application.Features.Authentication;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Repositories.Authentication;

public class AuthRepository(UserManager<AppUser> userManager, IJwtBearerHelper jwtBearerHelper) : IAuthRepository
{
    public async Task<AuthenticatedResponse> LoginAsync(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        
        if (user is null) throw new Exception("User not found.");

        if (!await userManager.CheckPasswordAsync(user, password)) throw new Exception("Invalid password.");
        
        var permissions = await userManager.GetRolesAsync(user);
        
        return jwtBearerHelper.GenerateToken(user.Id, user.UsuarioInfo, permissions);
    }

    public async Task RegisterAsync(RegisterUserCommand command)
    {
        var user = userManager.FindByNameAsync(command.username);
        
        if (user is not null) throw new Exception("Username already exists.");

        var newUser = new AppUser
        {
            Identificacion = command.identificacion,
            Nombre = command.nombre,
            Apellido = command.apellido,
            Sexo = command.sexo,
            RangoId = command.RangoId,
            DepartamentoId = command.DepartamentoId,
            UserName = command.username
        };
        
        await userManager.CreateAsync(newUser, command.password);
    }
}