namespace Application.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    public NotFoundException(string entidad, object clave) : this($"{entidad} '{clave}' no fue encontrada.") { }
}
