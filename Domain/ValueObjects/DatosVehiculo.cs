using Domain.Exceptions;

namespace Domain.ValueObjects;

/// <summary>
/// Vehículo tal como se registró en el evento. Admite vehículos sin placa y marca, modelo o
/// color fuera de catálogo (texto libre), como hacía SiGAV 1.0 con MarcaTxt/ModeloTxt/ColorTxt.
/// </summary>
public sealed record DatosVehiculo
{
    public const int PlacaMaxLength = 10;
    public const int TextoMaxLength = 50;

    public string? Placa { get; private init; }
    public int? TipoVehiculoId { get; private init; }
    public int? MarcaId { get; private init; }
    public int? ModeloId { get; private init; }
    public int? ColorId { get; private init; }
    public string? MarcaTexto { get; private init; }
    public string? ModeloTexto { get; private init; }
    public string? ColorTexto { get; private init; }

    // Requerido por EF Core
    private DatosVehiculo() { }

    public DatosVehiculo(
        string? placa,
        int? tipoVehiculoId,
        int? marcaId,
        int? modeloId,
        int? colorId,
        string? marcaTexto = null,
        string? modeloTexto = null,
        string? colorTexto = null)
    {
        foreach (var (id, campo) in new[] { (tipoVehiculoId, "tipo"), (marcaId, "marca"), (modeloId, "modelo"), (colorId, "color") })
            if (id is <= 0) throw new DomainException($"El {campo} de vehículo no es válido.");

        Placa = DatosPersona.Normalizar(placa, PlacaMaxLength, "placa", soloAlfanumericos: true);
        TipoVehiculoId = tipoVehiculoId;
        MarcaId = marcaId;
        ModeloId = modeloId;
        ColorId = colorId;
        // El texto libre solo aplica cuando el dato no está en el catálogo
        MarcaTexto = marcaId is null ? DatosPersona.Normalizar(marcaTexto, TextoMaxLength, "marca") : null;
        ModeloTexto = modeloId is null ? DatosPersona.Normalizar(modeloTexto, TextoMaxLength, "modelo") : null;
        ColorTexto = colorId is null ? DatosPersona.Normalizar(colorTexto, TextoMaxLength, "color") : null;

        // Un vehículo sin ningún dato no aporta nada (y se leería como "sin vehículo")
        if (Placa is null && TipoVehiculoId is null && MarcaId is null && ModeloId is null && ColorId is null
            && MarcaTexto is null && ModeloTexto is null && ColorTexto is null)
            throw new DomainException("Registre al menos un dato del vehículo (placa, tipo, marca, modelo o color).");
    }
}
