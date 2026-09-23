using System.Globalization;
using Domain.Exceptions;

namespace Domain.ValueObjects;

/// <summary>Punto geográfico (WGS84) donde ocurrió el evento.</summary>
public sealed record Coordenada
{
    public decimal Latitud { get; private init; }
    public decimal Longitud { get; private init; }

    // Requerido por EF Core
    private Coordenada() { }

    public Coordenada(decimal latitud, decimal longitud)
    {
        if (latitud is < -90 or > 90) throw new DomainException("La latitud debe estar entre -90 y 90.");
        if (longitud is < -180 or > 180) throw new DomainException("La longitud debe estar entre -180 y 180.");
        if (latitud == 0 && longitud == 0) throw new DomainException("La coordenada (0, 0) no es una ubicación válida.");

        // 6 decimales ≈ 11 cm: precisión suficiente y columna de tamaño fijo
        Latitud = decimal.Round(latitud, 6);
        Longitud = decimal.Round(longitud, 6);
    }

    /// <summary>Formato de SiGAV 1.0 y de la app: "latitud,longitud".</summary>
    public static Coordenada Parse(string texto)
    {
        var partes = (texto ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
        if (partes.Length != 2
            || !decimal.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitud)
            || !decimal.TryParse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitud))
            throw new DomainException("Las coordenadas deben tener el formato 'latitud,longitud'.");

        return new Coordenada(latitud, longitud);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Latitud},{Longitud}");
}
