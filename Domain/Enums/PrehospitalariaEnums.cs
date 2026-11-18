namespace Domain.Enums;

// Enums de la atención prehospitalaria. Empiezan en 1 a propósito: en SiGAV 1.0 la app y la web
// enviaban 1..n mientras el backend esperaba 0..n-1, y el despacho y el llenado capilar quedaron
// guardados con valores corridos.

/// <summary>Quién despachó la ambulancia (DepachoAsistenciaEnum en SiGAV 1.0).</summary>
public enum DespachoPrehospitalarioEnum
{
    Sistema911 = 1,
    Mopc = 2
}

/// <summary>TipoAsistenciaPreHospitalariaEnum en SiGAV 1.0.</summary>
public enum TipoAtencionPrehospitalariaEnum
{
    AsistenciaMedica = 1,
    /// <summary>Traslado programado o movimiento de un paciente sin emergencia en la vía.</summary>
    Movimiento = 2,
    /// <summary>Cobertura de un evento (festivo, deportivo...): requiere el nombre del evento.</summary>
    EventoEspecial = 3
}

/// <summary>TipoCausaPreHospitalariaEnum en SiGAV 1.0.</summary>
public enum CausaAtencionPrehospitalariaEnum
{
    Traumatica = 1,
    Clinica = 2
}

/// <summary>Por qué el paciente no fue trasladado (TipoCausaTrasladoEnum en SiGAV 1.0).</summary>
public enum MotivoNoTrasladoEnum
{
    NoAmeritaTraslado = 1,
    /// <summary>La víctima fue trasladada por el 911.</summary>
    TrasladadoPor911 = 2,
    RechazoAsistencia = 3,
    RechazoTraslado = 4,
    ApoyoAOtraUnidadAlfa = 5,
    ApoyoAl911 = 6
}

/// <summary>LlenadoCapilarEnum en SiGAV 1.0.</summary>
public enum LlenadoCapilarEnum
{
    /// <summary>Menor a 2 segundos.</summary>
    Normal = 1,
    /// <summary>Mayor a 2 segundos.</summary>
    Retrasado = 2
}
