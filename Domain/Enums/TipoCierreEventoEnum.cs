namespace Domain.Enums;

/// <summary>Cómo se cerró el evento (TipoCierreAsistenciaEnum en SiGAV 1.0, mismos valores).</summary>
public enum TipoCierreEventoEnum
{
    AsistidaPorMopc = 1,
    Transferida911 = 2,
    TransferidaPoliciaNacional = 3,
    TransferidaDigesett = 4,
    CiudadanoResolvio = 5,
    UnidadNoHizoContacto = 6,
    FueraDeJurisdiccion = 7
}
