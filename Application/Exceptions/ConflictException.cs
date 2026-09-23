namespace Application.Exceptions;

/// <summary>
/// El recurso ya existe o su estado impide la operación (p. ej. ficha o denominación duplicada).
/// </summary>
public class ConflictException(string message) : Exception(message);
