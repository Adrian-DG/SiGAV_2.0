namespace Domain.Exceptions;

/// <summary>
/// Violación de una regla de negocio (invariante) del dominio.
/// </summary>
public class DomainException(string message) : Exception(message);
