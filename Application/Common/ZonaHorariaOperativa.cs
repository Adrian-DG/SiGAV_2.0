namespace Application.Common;

/// <summary>
/// Las fechas se guardan en UTC, pero "un día" para las estadísticas es un día de operación en
/// República Dominicana (UTC-4, sin horario de verano), independiente de la zona del servidor.
/// </summary>
public static class ZonaHorariaOperativa
{
    public static readonly TimeZoneInfo Zona = ObtenerZona();

    public static DateOnly Hoy(TimeProvider timeProvider)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, Zona));

    /// <summary>Instante UTC en que empieza el día operativo indicado.</summary>
    public static DateTime InicioDelDiaUtc(DateOnly dia)
        => TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Zona);

    /// <summary>Rango semiabierto [desde, hasta + 1 día) en UTC, para filtrar días completos.</summary>
    public static (DateTime DesdeUtc, DateTime HastaExclusivoUtc) RangoUtc(DateOnly desde, DateOnly hasta)
        => (InicioDelDiaUtc(desde), InicioDelDiaUtc(hasta.AddDays(1)));

    private static TimeZoneInfo ObtenerZona()
    {
        // Id IANA (Linux/contenedores; Windows lo resuelve vía ICU). Si no existe, UTC-4 fijo.
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("AST-RD", TimeSpan.FromHours(-4), "Hora de República Dominicana", "AST"); }
    }
}
