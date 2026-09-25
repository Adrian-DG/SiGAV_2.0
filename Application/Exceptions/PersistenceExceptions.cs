namespace Application.Exceptions;

/// <summary>
/// La base de datos rechazó el guardado por un índice único (p. ej. dos peticiones simultáneas con
/// la misma clave de idempotencia o la misma ficha). Se responde 409 en lugar de 500.
/// </summary>
public class DuplicateKeyException(string message, Exception? innerException = null)
    : ConflictException(message)
{
    public Exception? Causa { get; } = innerException;
}

/// <summary>
/// Se referenció un registro que no existe (clave foránea), p. ej. un catálogo inválido que no
/// cubrió la validación. Se responde 400 en lugar de 500.
/// </summary>
public class InvalidReferenceException(string message, Exception? innerException = null) : Exception(message, innerException);
