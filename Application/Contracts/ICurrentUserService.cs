namespace Application.Contracts;

public interface ICurrentUserService
{
    /// <summary>
    /// Id del usuario autenticado, o null si la petición es anónima.
    /// </summary>
    int? UserId { get; }
}
