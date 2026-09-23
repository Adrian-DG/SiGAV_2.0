namespace Domain.Enums;

/// <summary>
/// Área a la que pertenece el agente (PerteneceA en SiGAV 1.0). En la app define qué
/// formularios de eventos puede reportar.
/// </summary>
public enum AreaOperativaEnum
{
    AsistenciaVial = 1,
    GestionOperativa = 2,
    SeguridadCiudadana = 3,
    Taller = 4,
    Gruas = 5,
    PreHospitalaria = 6,
    Rescate = 7
}
