namespace Application.Contracts;

/// <summary>
/// Datos del formulario de asistencia (APVI-FOR-003), ya resueltos y con las horas en la zona
/// operativa (República Dominicana). Los textos vacíos o null se dejan en blanco.
/// </summary>
public record ReporteAsistenciaDatos(
    int NumeroAsistencia,
    bool Anulado,
    DateTime FechaHoraReporte,
    // Afectado y ubicación (la persona principal: el primer conductor o, si no hay, la primera persona)
    string? Nombre,
    string? Apellido,
    string? Identificacion,
    int CantidadPersonas,
    string? Telefono,
    string? UbicacionAsistencia,
    string? Provincia,
    string? Municipio,
    string? Tramo,
    // Vehículo asistido (el de la persona principal o, si no iba en uno, el primero)
    string? Marca,
    string? Modelo,
    string? TipoVehiculo,
    string? Color,
    string? Placa,
    string? MotivoAsistencia,
    // Unidad patrullera (la principal del evento)
    string? Denominacion,
    string? FichaUnidad,
    string? OrdenadaPor,
    string? ReportadoPor,
    DateTime? HoraDespacho,
    DateTime? HoraLlegada,
    DateTime? HoraRetirada,
    string? TipoCierre,
    // Firmas
    string? Usuario,
    string? Observaciones,
    /// <summary>Los demás vehículos y personas del evento, una línea cada uno.</summary>
    IReadOnlyList<string> OtrosInvolucrados,
    /// <summary>Imágenes (JPEG, PNG o WebP) de las firmas capturadas como evidencia, o null.</summary>
    byte[]? FirmaCiudadano,
    byte[]? FirmaAgente);

/// <summary>Genera el formulario de asistencia de un evento en PDF.</summary>
public interface IReporteAsistencia
{
    public const string ContentType = "application/pdf";

    byte[] Generar(ReporteAsistenciaDatos datos);
}
