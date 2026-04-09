using Application.Contracts;
using Application.Contracts.Authentication;
using Infrastructure.Identity;
using Infrastructure.Repositories.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Data;

public class UnitOfWork(SiGAVContext context, UserManager<AppUser> userManager, IJwtBearerHelper jwtBearerHelper) : IUnitOfWork
{
    public IAuthRepository AuthRepository => new AuthRepository(userManager, jwtBearerHelper);
}