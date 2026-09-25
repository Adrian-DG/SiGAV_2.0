namespace Application.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    // "no existe" concuerda con cualquier género ("El evento '13' no existe", "La unidad '5' no existe");
    // el texto anterior ("no fue encontrada") solo servía para sustantivos femeninos.
    public NotFoundException(string entidad, object clave) : this($"{entidad} '{clave}' no existe.") { }
}
