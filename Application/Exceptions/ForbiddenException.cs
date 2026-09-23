namespace Application.Exceptions;

/// <summary>
/// La sesión es válida pero no le corresponde la operación o el recurso solicitado.
/// </summary>
public class ForbiddenException(string message) : Exception(message);
