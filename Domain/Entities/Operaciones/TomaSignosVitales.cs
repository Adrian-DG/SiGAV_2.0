using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Una toma de signos vitales del paciente, con su hora (inicial, durante el traslado, a la entrega...).
/// En SiGAV 1.0 había una sola toma, en JSON, con todos los valores obligatorios (se enviaba 0 cuando
/// no se medía); aquí cada valor es opcional pero los que se registran deben ser fisiológicamente posibles.
/// </summary>
public class TomaSignosVitales
{
    public const int FrecuenciaCardiacaMaxima = 300;
    public const int FrecuenciaRespiratoriaMaxima = 80;
    public const int TensionSistolicaMaxima = 300;
    public const int TensionDiastolicaMaxima = 200;
    public const decimal TemperaturaMinima = 25m;
    public const decimal TemperaturaMaxima = 45m;
    public const int GlucemiaMaxima = 1000;

    public const int GlasgowOcularMaximo = 4;
    public const int GlasgowVerbalMaximo = 5;
    public const int GlasgowMotorMaximo = 6;

    public DateTime TomadaUtc { get; private set; }

    /// <summary>Latidos por minuto (0: sin pulso).</summary>
    public int? FrecuenciaCardiaca { get; private set; }
    /// <summary>Respiraciones por minuto (0: apnea).</summary>
    public int? FrecuenciaRespiratoria { get; private set; }
    /// <summary>Tensión arterial sistólica, mmHg.</summary>
    public int? TensionSistolica { get; private set; }
    /// <summary>Tensión arterial diastólica, mmHg.</summary>
    public int? TensionDiastolica { get; private set; }
    /// <summary>Saturación de oxígeno (SpO2), %.</summary>
    public int? SaturacionOxigeno { get; private set; }
    /// <summary>°C, con un decimal (en SiGAV 1.0 era entero).</summary>
    public decimal? Temperatura { get; private set; }
    /// <summary>Glucemia capilar, mg/dL.</summary>
    public int? Glucemia { get; private set; }
    public LlenadoCapilarEnum? LlenadoCapilar { get; private set; }

    /// <summary>Escala de Coma de Glasgow: apertura ocular (1-4).</summary>
    public int? GlasgowOcular { get; private set; }
    /// <summary>Respuesta verbal (1-5).</summary>
    public int? GlasgowVerbal { get; private set; }
    /// <summary>Respuesta motora (1-6).</summary>
    public int? GlasgowMotor { get; private set; }

    /// <summary>Total de Glasgow (3-15); se registran los tres componentes o ninguno.</summary>
    public int? GlasgowTotal => GlasgowOcular + GlasgowVerbal + GlasgowMotor;

    // Requerido por EF Core
    private TomaSignosVitales() { }

    public static TomaSignosVitales Registrar(
        DateTime tomadaUtc,
        int? frecuenciaCardiaca = null,
        int? frecuenciaRespiratoria = null,
        int? tensionSistolica = null,
        int? tensionDiastolica = null,
        int? saturacionOxigeno = null,
        decimal? temperatura = null,
        int? glucemia = null,
        LlenadoCapilarEnum? llenadoCapilar = null,
        int? glasgowOcular = null,
        int? glasgowVerbal = null,
        int? glasgowMotor = null)
    {
        if (tomadaUtc.Kind == DateTimeKind.Local) tomadaUtc = tomadaUtc.ToUniversalTime();
        if (tomadaUtc == default) throw new DomainException("La hora de la toma de signos vitales es requerida.");

        Rango(frecuenciaCardiaca, 0, FrecuenciaCardiacaMaxima, "La frecuencia cardiaca");
        Rango(frecuenciaRespiratoria, 0, FrecuenciaRespiratoriaMaxima, "La frecuencia respiratoria");
        Rango(tensionSistolica, 0, TensionSistolicaMaxima, "La tensión sistólica");
        Rango(tensionDiastolica, 0, TensionDiastolicaMaxima, "La tensión diastólica");
        Rango(saturacionOxigeno, 0, 100, "La saturación de oxígeno");
        Rango(glucemia, 0, GlucemiaMaxima, "La glucemia");
        if (temperatura is < TemperaturaMinima or > TemperaturaMaxima)
            throw new DomainException($"La temperatura debe estar entre {TemperaturaMinima} y {TemperaturaMaxima} °C.");
        if (tensionSistolica is { } sistolica && tensionDiastolica is { } diastolica && sistolica > 0 && sistolica <= diastolica)
            throw new DomainException("La tensión sistólica debe ser mayor que la diastólica.");
        if (llenadoCapilar is { } llenado && !Enum.IsDefined(llenado))
            throw new DomainException("El llenado capilar no es válido.");

        var glasgow = new[] { glasgowOcular, glasgowVerbal, glasgowMotor };
        if (glasgow.Any(g => g is not null) && glasgow.Any(g => g is null))
            throw new DomainException("Registre los tres componentes de la escala de Glasgow (ocular, verbal y motor) o ninguno.");
        Rango(glasgowOcular, 1, GlasgowOcularMaximo, "La apertura ocular (Glasgow)");
        Rango(glasgowVerbal, 1, GlasgowVerbalMaximo, "La respuesta verbal (Glasgow)");
        Rango(glasgowMotor, 1, GlasgowMotorMaximo, "La respuesta motora (Glasgow)");

        var toma = new TomaSignosVitales
        {
            TomadaUtc = DateTime.SpecifyKind(tomadaUtc, DateTimeKind.Utc),
            FrecuenciaCardiaca = frecuenciaCardiaca,
            FrecuenciaRespiratoria = frecuenciaRespiratoria,
            TensionSistolica = tensionSistolica,
            TensionDiastolica = tensionDiastolica,
            SaturacionOxigeno = saturacionOxigeno,
            Temperatura = temperatura is { } t ? Math.Round(t, 1) : null,
            Glucemia = glucemia,
            LlenadoCapilar = llenadoCapilar,
            GlasgowOcular = glasgowOcular,
            GlasgowVerbal = glasgowVerbal,
            GlasgowMotor = glasgowMotor
        };

        if (!toma.TieneValores)
            throw new DomainException("La toma de signos vitales no tiene ningún valor.");

        return toma;
    }

    private bool TieneValores =>
        FrecuenciaCardiaca is not null || FrecuenciaRespiratoria is not null || TensionSistolica is not null
        || TensionDiastolica is not null || SaturacionOxigeno is not null || Temperatura is not null
        || Glucemia is not null || LlenadoCapilar is not null || GlasgowOcular is not null;

    private static void Rango(int? valor, int minimo, int maximo, string campo)
    {
        if (valor is { } v && (v < minimo || v > maximo))
            throw new DomainException($"{campo} debe estar entre {minimo} y {maximo}.");
    }
}
