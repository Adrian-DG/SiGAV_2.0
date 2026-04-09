using Application.Contracts.Authentication;

namespace Application.Contracts;

public interface IUnitOfWork
{ 
    IAuthRepository AuthRepository { get; }
}