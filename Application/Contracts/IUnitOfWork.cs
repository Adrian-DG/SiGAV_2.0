using Application.Contracts.Authentication;
using Application.Contracts.Operaciones;

namespace Application.Contracts;

public interface IUnitOfWork
{ 
    IAuthRepository AuthRepository { get; }
    IAgenteRepository AgenteRepository { get; }
}