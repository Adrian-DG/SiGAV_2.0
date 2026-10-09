namespace Application.Contracts;

/// <summary>Fila leída de una hoja: <see cref="Numero"/> es el número de fila en Excel (1 = primera).</summary>
public record FilaHoja(int Numero, IReadOnlyList<string> Celdas)
{
    public string Celda(int columna) => columna >= 0 && columna < Celdas.Count ? Celdas[columna] : string.Empty;
}

/// <summary>Hoja a escribir: la primera fila es el encabezado (en negrita).</summary>
public record HojaNueva(string Nombre, IReadOnlyList<IReadOnlyList<string>> Filas);

/// <summary>
/// Lectura y escritura de libros de Excel (.xlsx). Solo texto: lo necesario para las cargas
/// masivas y sus plantillas.
/// </summary>
public interface IHojaCalculo
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Filas no vacías de la primera hoja del libro, como texto.</summary>
    /// <exception cref="InvalidDataException">El archivo no es un .xlsx válido.</exception>
    IReadOnlyList<FilaHoja> LeerPrimeraHoja(Stream contenido);

    byte[] Escribir(IReadOnlyList<HojaNueva> hojas);
}
