namespace Application.Exceptions;

/// <summary>
/// Credenciales inválidas. El mensaje es genérico a propósito para no revelar qué dato falló.
/// </summary>
public class UnauthorizedException(string message = "Credenciales inválidas.") : Exception(message);
